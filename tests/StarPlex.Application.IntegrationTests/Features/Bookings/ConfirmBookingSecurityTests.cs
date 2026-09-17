using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MediatR;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBookingFromWebhook;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class ConfirmBookingSecurityTests : IntegrationTestBase
{
    public ConfirmBookingSecurityTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ConfirmBooking_ByNonOwnerCustomer_ReturnsFalse()
    {
        var ownerUserId = Guid.NewGuid();
        var nonOwnerUserId = Guid.NewGuid();

        var (booking, _) = await CreateTestBookingAsync(ownerUserId, "pi_stored", 150m);

        CurrentUserServiceMock.Setup(u => u.UserId).Returns(nonOwnerUserId);

        var request = new ConfirmBookingCommand
        {
            BookingId = booking.Id
        };

        var result = await Mediator.Send(request);

        result.Should().BeFalse();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task ConfirmBooking_WhenAlreadyConfirmedAndOwner_ReturnsTrueWithoutCallingStripe()
    {
        var ownerUserId = Guid.NewGuid();
        var (booking, _) = await CreateTestBookingAsync(ownerUserId, "pi_stored", 150m);

        booking.Status = BookingStatus.Confirmed;
        await DbContext.SaveChangesAsync(CancellationToken.None);

        CurrentUserServiceMock.Setup(u => u.UserId).Returns(ownerUserId);

        var request = new ConfirmBookingCommand
        {
            BookingId = booking.Id
        };

        var result = await Mediator.Send(request);

        result.Should().BeTrue();

        PaymentServiceMock.Verify(p => p.VerifyPaymentIntentAsync(
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<decimal>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmBookingFromWebhook_WithMismatchedPaymentIntentId_Rejects()
    {
        var ownerUserId = Guid.NewGuid();
        var (booking, _) = await CreateTestBookingAsync(ownerUserId, "pi_stored", 150m);

        var command = new ConfirmBookingFromWebhookCommand
        {
            BookingId = booking.Id,
            PaymentIntentId = "pi_mismatched_intent",
            Amount = 150m,
            Currency = "uah"
        };

        var result = await Mediator.Send(command);

        result.Should().BeFalse();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking!.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task ConfirmBookingFromWebhook_WithMismatchedAmountOrCurrency_Rejects()
    {
        var ownerUserId = Guid.NewGuid();
        var (booking, _) = await CreateTestBookingAsync(ownerUserId, "pi_stored", 150m);

        var commandWrongAmount = new ConfirmBookingFromWebhookCommand
        {
            BookingId = booking.Id,
            PaymentIntentId = "pi_stored",
            Amount = 500m,
            Currency = "uah"
        };

        var resultAmountMismatch = await Mediator.Send(commandWrongAmount);
        resultAmountMismatch.Should().BeFalse();

        var commandWrongCurrency = new ConfirmBookingFromWebhookCommand
        {
            BookingId = booking.Id,
            PaymentIntentId = "pi_stored",
            Amount = 150m,
            Currency = "usd"
        };

        var resultCurrencyMismatch = await Mediator.Send(commandWrongCurrency);
        resultCurrencyMismatch.Should().BeFalse();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking!.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task ConfirmBooking_WithInconsistentPersistedPaymentAmount_Rejects()
    {
        var ownerUserId = Guid.NewGuid();
        var (booking, _) = await CreateTestBookingAsync(ownerUserId, "pi_stored", 150m);

        // Corrupt persisted Payment.Amount so it does not match Booking.TotalPrice
        var payment = await DbContext.Payments.FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        payment!.Amount = 100m;
        await DbContext.SaveChangesAsync(CancellationToken.None);

        CurrentUserServiceMock.Setup(u => u.UserId).Returns(ownerUserId);

        var customerResult = await Mediator.Send(new ConfirmBookingCommand { BookingId = booking.Id });
        customerResult.Should().BeFalse();

        var webhookResult = await Mediator.Send(new ConfirmBookingFromWebhookCommand
        {
            BookingId = booking.Id,
            PaymentIntentId = "pi_stored",
            Amount = 150m,
            Currency = "uah"
        });
        webhookResult.Should().BeFalse();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking!.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task ConfirmBooking_ConcurrentCustomerAndWebhook_ExecutesSideEffectsOnlyOnce()
    {
        var ownerUserId = Guid.NewGuid();
        var (booking, seat) = await CreateTestBookingAsync(ownerUserId, "pi_stored", 150m);

        CurrentUserServiceMock.Setup(u => u.UserId).Returns(ownerUserId);

        PaymentServiceMock
            .Setup(p => p.VerifyPaymentIntentAsync("pi_stored", booking.Id, 150m, "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        UserServiceMock
            .Setup(u => u.GetUserContactInfoAsync(ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("customer@example.com", "Test Owner"));

        TicketServiceMock
            .Setup(t => t.GenerateTicketPdfAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        EmailServiceMock
            .Setup(e => e.SendTicketEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<byte[]>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Simulate concurrent requests across separate HTTP request scopes
        using var scope1 = ServiceProvider.CreateScope();
        using var scope2 = ServiceProvider.CreateScope();

        var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
        var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();

        var customerTask = mediator1.Send(new ConfirmBookingCommand { BookingId = booking.Id });
        var webhookTask = mediator2.Send(new ConfirmBookingFromWebhookCommand
        {
            BookingId = booking.Id,
            PaymentIntentId = "pi_stored",
            Amount = 150m,
            Currency = "uah"
        });

        var results = await Task.WhenAll(customerTask, webhookTask);

        results[0].Should().BeTrue();
        results[1].Should().BeTrue();

        var dbTickets = await DbContext.Tickets.AsNoTracking().Where(t => t.BookingSeat.BookingId == booking.Id).ToListAsync();
        dbTickets.Should().HaveCount(1);

        EmailServiceMock.Verify(e => e.SendTicketEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<List<byte[]>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task<(Booking Booking, Seat Seat)> CreateTestBookingAsync(Guid userId, string stripePaymentIntentId, decimal amount)
    {
        var cinema = new Cinema("Test Cinema", "Test Address", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "1", 1, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(12345, "Test Movie", "Test Movie", "Desc...", 120, "poster.jpg", "backdrop.jpg", "Action", "url", "PG-13", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, amount, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, amount, DateTime.UtcNow, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, stripePaymentIntentId, amount, PaymentStatus.Pending);
        DbContext.Payments.Add(payment);

        var temporaryLock = new SelectedSeat(session.Id, seat.Id, userId, DateTime.UtcNow.AddMinutes(10));
        DbContext.SelectedSeats.Add(temporaryLock);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        return (booking, seat);
    }
}
