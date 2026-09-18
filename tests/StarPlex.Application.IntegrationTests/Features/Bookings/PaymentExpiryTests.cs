using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using StarPlex.Application.Common.Models;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBookingFromWebhook;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Infrastructure.Persistence;
using StarPlex.Infrastructure.Services;
using System.Data.Common;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class PaymentExpiryTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;

    public PaymentExpiryTests(DatabaseFixture fixture) : base(fixture) => _fixture = fixture;

    [Fact]
    public async Task ExpiredPendingBooking_UnpaidPaymentIntent_CancelsIntentThenBooking()
    {
        var booking = await SeedAsync();
        PaymentServiceMock.Setup(p => p.ExpirePaymentIntentAsync("pi_expiry", It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                (await DbContext.Bookings.AsNoTracking().SingleAsync()).Status.Should().Be(BookingStatus.Pending);
                (await DbContext.SelectedSeats.CountAsync()).Should().Be(1);
                return PaymentIntentExpiryResult.Cancelled;
            });

        await CleanupAsync();

        await AssertStateAsync(BookingStatus.Cancelled, PaymentStatus.Pending, 0, 0);
        PaymentServiceMock.Verify(p => p.ExpirePaymentIntentAsync("pi_expiry", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExpiredPendingBooking_AlreadySucceededPayment_DoesNotCancelBooking()
    {
        var booking = await SeedAsync();
        PaymentServiceMock.Setup(p => p.ExpirePaymentIntentAsync("pi_expiry", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentIntentExpiryResult.AlreadySucceeded);

        await CleanupAsync();

        await AssertStateAsync(BookingStatus.Pending, PaymentStatus.Pending, 0, 1);
        using var scope = ServiceProvider.CreateScope();
        var confirmed = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new ConfirmBookingFromWebhookCommand
        {
            BookingId = booking.Id, PaymentIntentId = "pi_expiry", Amount = 150m, Currency = "uah"
        });
        confirmed.Should().BeTrue();
        await AssertStateAsync(BookingStatus.Confirmed, PaymentStatus.Succeeded, 1, 0);
    }

    [Theory]
    [InlineData(PaymentIntentExpiryResult.AlreadySucceeded)]
    [InlineData(PaymentIntentExpiryResult.Cancelled)]
    public async Task CleanupConcurrentWithConfirmation_DoesNotProducePaidCancelledState(PaymentIntentExpiryResult result)
    {
        var booking = await SeedAsync();
        var resolving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        PaymentServiceMock.Setup(p => p.ExpirePaymentIntentAsync("pi_expiry", It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                resolving.TrySetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(15));
                return result;
            });

        var cleanup = CleanupAsync();
        await resolving.Task.WaitAsync(TimeSpan.FromSeconds(10));
        using var scope = ServiceProvider.CreateScope();
        var confirmationDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await confirmationDb.Database.OpenConnectionAsync();
        var pid = ((NpgsqlConnection)confirmationDb.Database.GetDbConnection()).ProcessID;
        var confirmation = BookingConfirmationHelper.ExecuteAtomicConfirmationAsync(
            confirmationDb, booking.Id, "pi_expiry", 150m, "uah", NullLogger.Instance, default);
        try
        {
            await WaitForBlockedBackendAsync(pid);
            confirmation.IsCompleted.Should().BeFalse();
        }
        finally
        {
            release.TrySetResult();
            await cleanup;
        }

        var outcome = await confirmation;
        outcome.IsSuccess.Should().Be(result == PaymentIntentExpiryResult.AlreadySucceeded);
        await AssertStateAsync(
            result == PaymentIntentExpiryResult.AlreadySucceeded ? BookingStatus.Confirmed : BookingStatus.Cancelled,
            result == PaymentIntentExpiryResult.AlreadySucceeded ? PaymentStatus.Succeeded : PaymentStatus.Pending,
            result == PaymentIntentExpiryResult.AlreadySucceeded ? 1 : 0, 0);
        PaymentServiceMock.Verify(p => p.ExpirePaymentIntentAsync("pi_expiry", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmationWinsLock_CleanupReReadsConfirmedBookingAndSkipsProvider()
    {
        var booking = await SeedAsync();
        var gate = new BookingLockGate();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_fixture.ConnectionString).AddInterceptors(gate).Options;
        await using var confirmationDb = new ApplicationDbContext(options, CurrentUserServiceMock.Object);
        await confirmationDb.Database.OpenConnectionAsync();
        var pid = ((NpgsqlConnection)confirmationDb.Database.GetDbConnection()).ProcessID;
        var confirmation = BookingConfirmationHelper.ExecuteAtomicConfirmationAsync(
            confirmationDb, booking.Id, "pi_expiry", 150m, "uah", NullLogger.Instance, default);
        await gate.Acquired.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var cleanup = CleanupAsync();
        try
        {
            await WaitForBlockedBackendAsync(pid, waitForDependent: true);
            cleanup.IsCompleted.Should().BeFalse();
        }
        finally
        {
            gate.Release.TrySetResult();
            await confirmation;
            await cleanup;
        }

        (await confirmation).IsSuccess.Should().BeTrue();
        await AssertStateAsync(BookingStatus.Confirmed, PaymentStatus.Succeeded, 1, 0);
        PaymentServiceMock.Verify(p => p.ExpirePaymentIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PaymentProviderFailure_DoesNotCancelBooking_AndCanRetry()
    {
        await SeedAsync();
        PaymentServiceMock.SetupSequence(p => p.ExpirePaymentIntentAsync("pi_expiry", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentIntentExpiryResult.Indeterminate)
            .ReturnsAsync(PaymentIntentExpiryResult.Cancelled);

        await CleanupAsync();
        await AssertStateAsync(BookingStatus.Pending, PaymentStatus.Pending, 0, 1);
        await CleanupAsync();
        await AssertStateAsync(BookingStatus.Cancelled, PaymentStatus.Pending, 0, 0);
        PaymentServiceMock.Verify(p => p.ExpirePaymentIntentAsync("pi_expiry", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingPersistedIntent_DoesNotCancelAnUnresolvedPaymentAttempt(bool missingPayment)
    {
        await SeedAsync();
        var payment = await DbContext.Payments.SingleAsync();
        if (missingPayment) DbContext.Payments.Remove(payment);
        else payment.StripePaymentIntentId = string.Empty;
        await DbContext.SaveChangesAsync(default);

        await CleanupAsync();

        (await DbContext.Bookings.AsNoTracking().SingleAsync()).Status.Should().Be(BookingStatus.Pending);
        (await DbContext.SelectedSeats.CountAsync()).Should().Be(1);
        (await DbContext.Tickets.CountAsync()).Should().Be(0);
        PaymentServiceMock.Verify(p => p.ExpirePaymentIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task<Booking> SeedAsync()
    {
        var owner = Guid.NewGuid();
        CurrentUserServiceMock.SetupGet(u => u.UserId).Returns(owner);
        var cinema = new Cinema("Expiry", "Test", "Kyiv");
        var hall = new Hall(cinema.Id, "Hall", 1, 1);
        var seat = new Seat(hall.Id, "1", 1, SeatType.Standard);
        var movie = new Movie(2100, "Expiry", "Expiry", "Desc", 90, "p", "b", "Drama", "u", "PG", 8, MovieStatus.NowShowing, DateTime.UtcNow);
        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 90, 150, 150, SessionStatus.Active);
        var booking = new Booking(owner, session.Id, 150, DateTime.UtcNow.AddMinutes(-20), BookingStatus.Pending);
        booking.BookingSeats.Add(new BookingSeat(booking.Id, seat.Id, 150));
        DbContext.Cinemas.Add(cinema);
        DbContext.Halls.Add(hall);
        DbContext.Seats.Add(seat);
        DbContext.Movies.Add(movie);
        DbContext.Sessions.Add(session);
        DbContext.Bookings.Add(booking);
        DbContext.Payments.Add(new Payment(booking.Id, "pi_expiry", 150, PaymentStatus.Pending));
        DbContext.SelectedSeats.Add(new SelectedSeat(session.Id, seat.Id, owner, DateTime.UtcNow.AddMinutes(5)));
        await DbContext.SaveChangesAsync(default);
        ((DbContext)DbContext).ChangeTracker.Clear();
        return booking;
    }

    private async Task CleanupAsync()
    {
        using var cleanup = new ExpiredLocksCleanupService(ServiceProvider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ExpiredLocksCleanupService>.Instance);
        await cleanup.CleanExpiredLocksAndBookingsAsync();
    }

    private async Task AssertStateAsync(BookingStatus booking, PaymentStatus payment, int tickets, int locks)
    {
        (await DbContext.Bookings.AsNoTracking().SingleAsync()).Status.Should().Be(booking);
        (await DbContext.Payments.AsNoTracking().SingleAsync()).Status.Should().Be(payment);
        (await DbContext.Tickets.CountAsync()).Should().Be(tickets);
        (await DbContext.SelectedSeats.CountAsync()).Should().Be(locks);
        (await DbContext.BookingSeats.CountAsync()).Should().Be(1);
    }

    private async Task WaitForBlockedBackendAsync(int pid, bool waitForDependent = false)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        var sql = waitForDependent
            ? "SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE @pid = ANY(pg_blocking_pids(pid)))"
            : "SELECT cardinality(pg_blocking_pids(@pid)) > 0";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("pid", pid);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!(bool)(await command.ExecuteScalarAsync(timeout.Token))!)
            await Task.Delay(20, timeout.Token);
    }

    private sealed class BookingLockGate : DbCommandInterceptor
    {
        public TaskCompletionSource Acquired { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal))
            {
                Acquired.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }
}
