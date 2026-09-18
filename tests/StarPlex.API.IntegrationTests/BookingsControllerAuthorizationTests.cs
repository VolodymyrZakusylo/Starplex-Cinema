using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarPlex.API.Controllers;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Infrastructure.Persistence;

namespace StarPlex.API.IntegrationTests;

public class BookingsControllerAuthorizationTests
{
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITicketService> _ticketServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, _currentUserServiceMock.Object);
    }

    private BookingsController CreateController(ApplicationDbContext dbContext, string role)
    {
        var controller = new BookingsController(_mediatorMock.Object, _ticketServiceMock.Object, dbContext, _currentUserServiceMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, _callerId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                }, "Test"))
            }
        };
        return controller;
    }

    private async Task<(ApplicationDbContext Db, Guid CinemaAId, Guid CinemaBId, Guid BookingAId, Guid TicketAId)> Seed(bool customerOwned = false)
    {
        var db = CreateDbContext();
        var cinemaA = new Cinema("Cinema A", "Address A", "Kyiv");
        var cinemaB = new Cinema("Cinema B", "Address B", "Kyiv");
        var hallA = new Hall(cinemaA.Id, "Hall A1", 2, 2);
        var hallB = new Hall(cinemaB.Id, "Hall B1", 2, 2);
        var movie = new Movie(101, "Test Movie", "Test Movie", "Desc", 90, "p.jpg", "b.jpg", "Drama", "url", "PG", 8, MovieStatus.NowShowing, DateTime.UtcNow);
        var sessionA = new Session(movie.Id, hallA.Id, DateTime.UtcNow.AddHours(2), 90, 150, 150, SessionStatus.Active);
        var seatA = new Seat(hallA.Id, "1", 1, SeatType.Standard);

        var bookingA = new Booking(customerOwned ? _callerId : Guid.Empty, sessionA.Id, 150, DateTime.UtcNow, BookingStatus.Confirmed);
        var bookingSeatA = new BookingSeat(bookingA.Id, seatA.Id, 150m);
        bookingA.BookingSeats.Add(bookingSeatA);
        var ticketA = new Ticket(bookingSeatA.Id, "TICKET-A");

        db.Cinemas.AddRange(cinemaA, cinemaB);
        db.Halls.AddRange(hallA, hallB);
        db.Movies.Add(movie);
        db.Seats.Add(seatA);
        db.Sessions.Add(sessionA);
        db.Bookings.Add(bookingA);
        db.Tickets.Add(ticketA);
        await db.SaveChangesAsync();

        return (db, cinemaA.Id, cinemaB.Id, bookingA.Id, ticketA.Id);
    }

    [Theory]
    [InlineData("Cashier", "same", false, true)]
    [InlineData("Cashier", "other", false, false)]
    [InlineData("Cashier", "missing", false, false)]
    [InlineData("CinemaManager", "same", false, true)]
    [InlineData("CinemaManager", "other", false, false)]
    [InlineData("CinemaManager", "missing", false, false)]
    [InlineData("SuperAdmin", "missing", false, true)]
    [InlineData("Customer", "missing", true, true)]
    [InlineData("Customer", "same", false, false)]
    [InlineData("Cashier", "other", true, true)]
    public async Task DownloadAllTickets_EnforcesOwnershipAndCinemaScope(string role, string scope, bool owner, bool allowed)
    {
        var (db, cinemaAId, cinemaBId, bookingAId, ticketAId) = await Seed(customerOwned: owner);

        _currentUserServiceMock.SetupGet(u => u.UserId).Returns(_callerId);
        _currentUserServiceMock.SetupGet(u => u.Role).Returns(role);
        _currentUserServiceMock.SetupGet(u => u.IsSuperAdmin).Returns(role == "SuperAdmin");
        _currentUserServiceMock.SetupGet(u => u.CinemaId).Returns(scope == "same" ? cinemaAId : scope == "other" ? cinemaBId : null);

        _ticketServiceMock.Setup(t => t.GenerateTicketsPdfAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var controller = CreateController(db, role);
        var result = await controller.DownloadAllTickets(bookingAId, default);

        if (allowed)
        {
            result.Should().BeOfType<FileContentResult>();
            _ticketServiceMock.Verify(t => t.GenerateTicketsPdfAsync(
                It.Is<List<Guid>>(ids => ids.Count == 1 && ids.Contains(ticketAId)), It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            result.Should().BeOfType<ForbidResult>();
            _ticketServiceMock.Invocations.Should().BeEmpty();
        }
    }
}
