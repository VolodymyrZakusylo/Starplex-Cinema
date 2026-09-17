using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Helpers;
using StarPlex.Application.Features.Bookings.Commands.ScanTicket;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class ScanTicketCommandHandlerTests : IntegrationTestBase
{
    public ScanTicketCommandHandlerTests(DatabaseFixture fixture) : base(fixture)
    {
        CurrentUserServiceMock.Setup(u => u.IsSuperAdmin).Returns(true);
    }

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

        var expectedKyivTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(session.StartTime, DateTimeKind.Utc), TimeZoneHelpers.KyivTimeZone);
        result.StartTime.Should().Be(expectedKyivTime.ToString("HH:mm"));

        var dbTicket = await DbContext.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticket.Id);
        dbTicket.Should().NotBeNull();
        dbTicket!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ScanTicket_SummerDate_ShouldReturnKyivLocalTimeEEST()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex Summer", "Main St 2", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Summer Hall", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "B", 1, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(12346, "Summer Movie", "Summer Movie", "Desc", 120, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        // Summer date: 15:00 UTC on July 15 (EEST, UTC+3 -> 18:00 Kyiv time)
        var summerUtcStartTime = new DateTime(2026, 7, 15, 15, 0, 0, DateTimeKind.Utc);
        int durationMinutes = (int)Math.Ceiling((DateTime.UtcNow - summerUtcStartTime).TotalMinutes) + 120;

        var session = new Session(movie.Id, hall.Id, summerUtcStartTime, durationMinutes, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 150m, DateTime.UtcNow, BookingStatus.Confirmed)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var ticket = new Ticket(bookingSeat.Id, "SPX-SUMMER-TICKET", isUsed: false);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var request = new ScanTicketCommand { TicketCode = "SPX-SUMMER-TICKET" };

        var result = await Mediator.Send(request);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.StartTime.Should().Be("18:00");

        var dbTicket = await DbContext.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticket.Id);
        dbTicket.Should().NotBeNull();
        dbTicket!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ScanTicket_WinterDate_ShouldReturnKyivLocalTimeEET()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex Winter", "Main St 3", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Winter Hall", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "C", 2, SeatType.Standard);
        DbContext.Seats.Add(seat);

        var movie = new Movie(12347, "Winter Movie", "Winter Movie", "Desc", 120, "p.jpg", "b.jpg", "Drama", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        // Winter date: 15:00 UTC on January 15 (EET, UTC+2 -> 17:00 Kyiv time)
        var winterUtcStartTime = new DateTime(2026, 1, 15, 15, 0, 0, DateTimeKind.Utc);
        int durationMinutes = (int)Math.Ceiling((DateTime.UtcNow - winterUtcStartTime).TotalMinutes) + 120;

        var session = new Session(movie.Id, hall.Id, winterUtcStartTime, durationMinutes, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 150m, DateTime.UtcNow, BookingStatus.Confirmed)
        {
            Id = Guid.NewGuid()
        };
        var bookingSeat = new BookingSeat(booking.Id, seat.Id) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var ticket = new Ticket(bookingSeat.Id, "SPX-WINTER-TICKET", isUsed: false);
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var request = new ScanTicketCommand { TicketCode = "SPX-WINTER-TICKET" };

        var result = await Mediator.Send(request);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.StartTime.Should().Be("17:00");

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
