using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Features.Bookings.Commands.ScanTicket;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class ScanTicketCommandHandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task ScanTicket_WhenTicketCodeIsValid_ShouldMarkTicketAsUsedAndReturnSuccessDto()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Main Hall", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "A", 5, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(12345, "Inception", "Inception", "A thief...", 120, "poster.jpg", "backdrop.jpg", "Sci-Fi", "url", "PG-13", 8.8, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddMinutes(5), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 150m, DateTime.UtcNow, BookingStatus.Confirmed)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var ticket = new Ticket(bookingSeat.Id, "SPX-VALID-TICKET", isUsed: false);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var request = new ScanTicketCommand
        {
            TicketCode = "SPX-VALID-TICKET"
        };

        var result = await Mediator.Send(request);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Access Granted! Ticket successfully checked in.");
        result.MovieTitle.Should().Be(movie.Title);
        result.HallName.Should().Be(hall.Name);
        result.Row.Should().Be("A");
        result.Number.Should().Be(5);
        result.StartTime.Should().Be(session.StartTime.ToString("HH:mm"));

        var dbTicket = await DbContext.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticket.Id);
        dbTicket.Should().NotBeNull();
        dbTicket!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ScanTicket_WhenTicketCodeDoesNotExist_ShouldReturnFalseWithNotFoundMessage()
    {
        var request = new ScanTicketCommand
        {
            TicketCode = "NON-EXISTENT-CODE"
        };

        var result = await Mediator.Send(request);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Ticket not found in the StarPlex database.");
    }
}
