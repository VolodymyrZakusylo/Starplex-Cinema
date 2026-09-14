using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarPlex.Application.Features.Bookings.Commands.CancelTicket;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class CancelTicketCommandHandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task CancelTicket_WhenLastTicketIsCancelled_ShouldRefundPaymentAndCancelBooking()
    {
        var userId = Guid.NewGuid();

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
        var bookingSeat = new BookingSeat(booking.Id, seat.Id) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_test", 150m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket = new Ticket(bookingSeat.Id, "CODE123");
        DbContext.Tickets.Add(ticket);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_test", It.IsAny<decimal>(), "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CancelTicketCommand
        {
            UserId = userId,
            TicketId = ticket.Id
        };

        var result = await Mediator.Send(request);

        result.Should().BeTrue();

        var dbTickets = await DbContext.Tickets.AsNoTracking().ToListAsync();
        dbTickets.Should().BeEmpty();

        var dbBookingSeats = await DbContext.BookingSeats.AsNoTracking().ToListAsync();
        dbBookingSeats.Should().BeEmpty();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Cancelled);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.Status.Should().Be(PaymentStatus.Refunded);

        PaymentServiceMock.Verify(
            p => p.RefundPaymentAsync("pi_test", 150m, "uah", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelTicket_WhenOtherTicketsRemain_ShouldDeleteTargetTicketAndKeepBookingActive()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Main Hall", 10, 10);
        DbContext.Halls.Add(hall);

        var seat1 = new Seat(hall.Id, "1", 1, SeatType.Standard);
        var seat2 = new Seat(hall.Id, "1", 2, SeatType.Standard);
        DbContext.Seats.AddRange(seat1, seat2);

        var movie = new Movie(12345, "Inception", "Inception", "A thief...", 120, "poster.jpg", "backdrop.jpg", "Sci-Fi", "url", "PG-13", 8.8, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 300m, DateTime.UtcNow, BookingStatus.Confirmed)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat1 = new BookingSeat(booking.Id, seat1.Id) { Id = Guid.NewGuid() };
        var bookingSeat2 = new BookingSeat(booking.Id, seat2.Id) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat1);
        booking.BookingSeats.Add(bookingSeat2);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_test_2", 300m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket1 = new Ticket(bookingSeat1.Id, "CODE1");
        var ticket2 = new Ticket(bookingSeat2.Id, "CODE2");
        DbContext.Tickets.AddRange(ticket1, ticket2);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_test_2", It.IsAny<decimal>(), "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CancelTicketCommand
        {
            UserId = userId,
            TicketId = ticket1.Id
        };

        var result = await Mediator.Send(request);

        result.Should().BeTrue();

        var dbTickets = await DbContext.Tickets.AsNoTracking().ToListAsync();
        dbTickets.Should().HaveCount(1);
        dbTickets.First().Id.Should().Be(ticket2.Id);

        var dbBookingSeats = await DbContext.BookingSeats.AsNoTracking().ToListAsync();
        dbBookingSeats.Should().HaveCount(1);
        dbBookingSeats.First().Id.Should().Be(bookingSeat2.Id);

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Confirmed);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public async Task CancelTicket_WhenStripeRefundFails_ShouldReturnFalseAndNotModifyDatabase()
    {
        var userId = Guid.NewGuid();

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
        var bookingSeat = new BookingSeat(booking.Id, seat.Id) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_test_fail", 150m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket = new Ticket(bookingSeat.Id, "CODE123");
        DbContext.Tickets.Add(ticket);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_test_fail", It.IsAny<decimal>(), "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CancelTicketCommand
        {
            UserId = userId,
            TicketId = ticket.Id
        };

        var result = await Mediator.Send(request);

        result.Should().BeFalse();

        var dbTickets = await DbContext.Tickets.AsNoTracking().ToListAsync();
        dbTickets.Should().HaveCount(1);
        dbTickets.First().Id.Should().Be(ticket.Id);

        var dbBookingSeats = await DbContext.BookingSeats.AsNoTracking().ToListAsync();
        dbBookingSeats.Should().HaveCount(1);
        dbBookingSeats.First().Id.Should().Be(bookingSeat.Id);

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Confirmed);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.Status.Should().Be(PaymentStatus.Succeeded);
    }
}
