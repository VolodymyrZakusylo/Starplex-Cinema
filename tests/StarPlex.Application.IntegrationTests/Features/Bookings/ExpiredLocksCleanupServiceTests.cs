using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StarPlex.Application.Common.Models;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Infrastructure.Services;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class ExpiredLocksCleanupServiceTests : IntegrationTestBase
{
    public ExpiredLocksCleanupServiceTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CleanExpiredLocksAndBookings_WhenBookingIsPendingAndExpired_ShouldCancelBookingAndRemoveSeats()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex Cleanup 1", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "A", 1, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(2001, "Cleanup Movie 1", "Cleanup Movie 1", "Desc", 120, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddHours(2), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var expiredBookingTime = DateTime.UtcNow.AddMinutes(-20);
        var booking = new Booking(userId, session.Id, 150m, expiredBookingTime, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var selectedSeat = new SelectedSeat(session.Id, seat.Id, userId, DateTime.UtcNow.AddMinutes(5));
        DbContext.SelectedSeats.Add(selectedSeat);
        DbContext.Payments.Add(new Payment(booking.Id, "pi_cleanup_unpaid", 150m, PaymentStatus.Pending));
        PaymentServiceMock.Setup(p => p.ExpirePaymentIntentAsync("pi_cleanup_unpaid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentIntentExpiryResult.Cancelled);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var cleanupService = new ExpiredLocksCleanupService(scopeFactory, NullLogger<ExpiredLocksCleanupService>.Instance);

        await cleanupService.CleanExpiredLocksAndBookingsAsync();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Cancelled);

        var remainingLocks = await DbContext.SelectedSeats.AsNoTracking()
            .Where(ss => ss.SessionId == session.Id && ss.SeatId == seat.Id && ss.UserId == userId)
            .ToListAsync();
        remainingLocks.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanExpiredLocksAndBookings_WhenBookingIsAlreadyConfirmed_ShouldPreserveConfirmedStatus()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex Cleanup 2", "Main St 2", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 2", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "B", 2, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(2002, "Cleanup Movie 2", "Cleanup Movie 2", "Desc", 120, "p.jpg", "b.jpg", "Drama", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddHours(2), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var oldBookingTime = DateTime.UtcNow.AddMinutes(-20);
        var booking = new Booking(userId, session.Id, 150m, oldBookingTime, BookingStatus.Confirmed)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var cleanupService = new ExpiredLocksCleanupService(scopeFactory, NullLogger<ExpiredLocksCleanupService>.Instance);

        await cleanupService.CleanExpiredLocksAndBookingsAsync();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task CleanExpiredLocksAndBookings_WhenBookingIsAlreadyCancelled_ConfirmationShouldFail()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex Cleanup 3", "Main St 3", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 3", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "C", 3, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(2003, "Cleanup Movie 3", "Cleanup Movie 3", "Desc", 120, "p.jpg", "b.jpg", "Thriller", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddHours(2), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var expiredBookingTime = DateTime.UtcNow.AddMinutes(-20);
        var booking = new Booking(userId, session.Id, 150m, expiredBookingTime, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);

        var payment = new Payment(booking.Id, "pi_cleanup_win_1", 150m, PaymentStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        DbContext.Payments.Add(payment);
        PaymentServiceMock.Setup(p => p.ExpirePaymentIntentAsync("pi_cleanup_win_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentIntentExpiryResult.Cancelled);
        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var cleanupService = new ExpiredLocksCleanupService(scopeFactory, NullLogger<ExpiredLocksCleanupService>.Instance);

        await cleanupService.CleanExpiredLocksAndBookingsAsync();

        ((DbContext)DbContext).ChangeTracker.Clear();

        var (confirmSuccess, newlyConfirmed) = await BookingConfirmationHelper.ExecuteAtomicConfirmationAsync(
            DbContext,
            booking.Id,
            "pi_cleanup_win_1",
            150m,
            "UAH",
            NullLogger.Instance,
            CancellationToken.None);

        confirmSuccess.Should().BeFalse();
        newlyConfirmed.Should().BeFalse();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Cancelled);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.Status.Should().NotBe(PaymentStatus.Succeeded);

        var tickets = await DbContext.Tickets.AsNoTracking()
            .Where(t => t.BookingSeatId == bookingSeat.Id)
            .ToListAsync();
        tickets.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanExpiredLocksAndBookings_WhenBookingConfirmedAfterCandidateDiscovery_RevalidatesStateUnderLockAndDoesNotCancel()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex Cleanup Contention", "Main St 4", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 4", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "D", 4, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(2004, "Contention Movie", "Contention Movie", "Desc", 120, "p.jpg", "b.jpg", "Sci-Fi", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddHours(2), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var expiredBookingTime = DateTime.UtcNow.AddMinutes(-20);
        var booking = new Booking(userId, session.Id, 150m, expiredBookingTime, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);

        var payment = new Payment(booking.Id, "pi_contention_1", 150m, PaymentStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        DbContext.Payments.Add(payment);
        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var candidateBookingIds = await DbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Pending && b.BookingTime <= expiredBookingTime)
            .Select(b => b.Id)
            .ToListAsync();

        candidateBookingIds.Should().Contain(booking.Id);

        var (confirmSuccess, newlyConfirmed) = await BookingConfirmationHelper.ExecuteAtomicConfirmationAsync(
            DbContext,
            booking.Id,
            "pi_contention_1",
            150m,
            "UAH",
            NullLogger.Instance,
            CancellationToken.None);

        confirmSuccess.Should().BeTrue();
        newlyConfirmed.Should().BeTrue();

        var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var cleanupService = new ExpiredLocksCleanupService(scopeFactory, NullLogger<ExpiredLocksCleanupService>.Instance);

        await cleanupService.CleanExpiredLocksAndBookingsAsync();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Confirmed);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.Status.Should().Be(PaymentStatus.Succeeded);

        var tickets = await DbContext.Tickets.AsNoTracking()
            .Where(t => t.BookingSeatId == bookingSeat.Id)
            .ToListAsync();
        tickets.Should().HaveCount(1);

        (dbBooking.Status == BookingStatus.Cancelled && dbPayment.Status == PaymentStatus.Succeeded && tickets.Any())
            .Should().BeFalse();
    }
}
