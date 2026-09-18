using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class ConfirmBookingCommandHandlerTests : IntegrationTestBase
{
    public ConfirmBookingCommandHandlerTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ConfirmBooking_WhenBookingIsValid_ShouldConfirmBookingAndGenerateTickets()
    {
        var userId = Guid.NewGuid();
        CurrentUserServiceMock.Setup(u => u.UserId).Returns(userId);

        var cinema = new Cinema("StarPlex", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Main Hall", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "1", 1, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(12345, "Inception", "Inception", "A thief...", 120, "poster.jpg", "backdrop.jpg", "Sci-Fi", "url", "PG-13", 8.8, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 150m, DateTime.UtcNow, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_test", 150m, PaymentStatus.Pending);
        DbContext.Payments.Add(payment);

        var temporaryLock = new SelectedSeat(session.Id, seat.Id, userId, DateTime.UtcNow.AddMinutes(10));
        DbContext.SelectedSeats.Add(temporaryLock);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.VerifyPaymentIntentAsync("pi_test", booking.Id, 150m, "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new ConfirmBookingCommand
        {
            BookingId = booking.Id
        };

        UserServiceMock
            .Setup(u => u.GetUserContactInfoAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("test@example.com", "Test User"));

        TicketServiceMock
            .Setup(t => t.GenerateTicketPdfAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        EmailServiceMock
            .Setup(e => e.SendTicketEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<byte[]>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await Mediator.Send(request);

        result.Should().BeTrue();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Confirmed);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.Status.Should().Be(PaymentStatus.Succeeded);
        dbPayment.PaidAt.Should().NotBeNull();

        var dbSelectedSeats = await DbContext.SelectedSeats.AsNoTracking().Where(ss => ss.SessionId == session.Id).ToListAsync();
        dbSelectedSeats.Should().BeEmpty();

        var dbTickets = await DbContext.Tickets.AsNoTracking().Where(t => t.BookingSeatId == bookingSeat.Id).ToListAsync();
        dbTickets.Should().HaveCount(1);
        dbTickets.First().BookingSeatId.Should().Be(bookingSeat.Id);
    }

    [Fact]
    public async Task ConfirmBooking_WhenBookingDoesNotExist_ShouldReturnFalse()
    {
        CurrentUserServiceMock.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var request = new ConfirmBookingCommand
        {
            BookingId = Guid.NewGuid()
        };

        var result = await Mediator.Send(request);

        result.Should().BeFalse();

        var dbBookings = await DbContext.Bookings.AsNoTracking().CountAsync();
        dbBookings.Should().Be(0);
    }

    [Fact]
    public async Task ConfirmBooking_WhenBookingIsAlreadyConfirmed_ShouldReturnTrueAndMakeNoChanges()
    {
        var userId = Guid.NewGuid();
        CurrentUserServiceMock.Setup(u => u.UserId).Returns(userId);

        var cinema = new Cinema("StarPlex", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Main Hall", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "1", 1, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(12345, "Inception", "Inception", "A thief...", 120, "poster.jpg", "backdrop.jpg", "Sci-Fi", "url", "PG-13", 8.8, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 150m, DateTime.UtcNow, BookingStatus.Confirmed)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_test", 150m, PaymentStatus.Succeeded)
        {
            PaidAt = DateTime.UtcNow
        };
        DbContext.Payments.Add(payment);

        var ticket = new Ticket(bookingSeat.Id, "CODE123", false);
        DbContext.Tickets.Add(ticket);

        var temporaryLock = new SelectedSeat(session.Id, seat.Id, userId, DateTime.UtcNow.AddMinutes(10));
        DbContext.SelectedSeats.Add(temporaryLock);
        await DbContext.SaveChangesAsync(CancellationToken.None);

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

        var dbTickets = await DbContext.Tickets.AsNoTracking().Where(t => t.BookingSeatId == bookingSeat.Id).ToListAsync();
        dbTickets.Should().HaveCount(1);
        dbTickets.First().Id.Should().Be(ticket.Id);

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Confirmed);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.Status.Should().Be(PaymentStatus.Succeeded);

        var dbSelectedSeats = await DbContext.SelectedSeats.AsNoTracking().Where(ss => ss.SessionId == session.Id).ToListAsync();
        dbSelectedSeats.Should().HaveCount(1);
    }
}
