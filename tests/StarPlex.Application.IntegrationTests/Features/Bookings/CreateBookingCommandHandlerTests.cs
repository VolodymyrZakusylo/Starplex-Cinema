using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Features.Bookings.Commands.CreateBooking;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class CreateBookingCommandHandlerTests : IntegrationTestBase
{
    public CreateBookingCommandHandlerTests(DatabaseFixture fixture) : base(fixture)
    {
    }
    [Fact]
    public async Task CreateBooking_ShouldCreateBookingSuccessfully()
    {
        var userId = Guid.NewGuid();
        
        var cinema = new Cinema("StarPlex", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Main Hall", 10, 10);
        DbContext.Halls.Add(hall);
        
        var seat1 = new Seat(hall.Id, "1", 1, SeatType.Standard);
        var seat2 = new Seat(hall.Id, "1", 2, SeatType.Standard);
        DbContext.Seats.AddRange(seat1, seat2);

        var movie = new Movie(12345, "Inception", "Inception", "A thief who steals corporate secrets...", 120, "poster.jpg", "backdrop.jpg", "Sci-Fi", "url", "PG-13", 8.8, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var lock1 = new SelectedSeat(session.Id, seat1.Id, userId, DateTime.UtcNow.AddMinutes(10));
        var lock2 = new SelectedSeat(session.Id, seat2.Id, userId, DateTime.UtcNow.AddMinutes(10));
        DbContext.SelectedSeats.AddRange(lock1, lock2);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var request = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seat1.Id, seat2.Id },
            PromoCode = null
        };

        SeatLockServiceMock
            .Setup(s => s.GetLockedSeatIdsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { seat1.Id, seat2.Id });

        PaymentServiceMock
            .Setup(p => p.CreatePaymentIntentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync("pi_test_123");

        var result = await Mediator.Send(request);

        result.Should().NotBeNull();
        result.BookingId.Should().NotBeEmpty();

        var createdBooking = await DbContext.Bookings
            .Include(b => b.BookingSeats)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == result.BookingId);

        createdBooking.Should().NotBeNull();
        createdBooking!.UserId.Should().Be(userId);
        createdBooking.SessionId.Should().Be(session.Id);
        createdBooking.BookingSeats.Should().HaveCount(2);
        
        createdBooking.Payment.Should().NotBeNull();
        createdBooking.Payment!.StripePaymentIntentId.Should().Be("pi_test_123");
        createdBooking.Payment.Status.Should().Be(PaymentStatus.Pending);
        
        var bookingSeatIds = createdBooking.BookingSeats.Select(bs => bs.SeatId).ToList();
        bookingSeatIds.Should().Contain(new[] { seat1.Id, seat2.Id });
    }

    [Fact]
    public async Task CreateBooking_WhenSessionDoesNotExist_ShouldThrowNotFoundException()
    {
        var request = new CreateBookingCommand
        {
            SessionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SeatIds = new List<Guid> { Guid.NewGuid() },
            PromoCode = null
        };

        var act = async () => await Mediator.Send(request);

        await act.Should().ThrowAsync<NotFoundException>();

        var dbBookings = await DbContext.Bookings.CountAsync();
        dbBookings.Should().Be(0);
    }

    [Fact]
    public async Task CreateBooking_WhenSeatLockIsExpired_ShouldThrowBusinessRuleException()
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

        var expiredLock = new SelectedSeat(session.Id, seat.Id, userId, DateTime.UtcNow.AddMinutes(-5));
        DbContext.SelectedSeats.Add(expiredLock);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var request = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seat.Id },
            PromoCode = null
        };

        SeatLockServiceMock
            .Setup(s => s.GetLockedSeatIdsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var act = async () => await Mediator.Send(request);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Your reservation session for some of these seats has expired or is invalid.");

        var dbBookings = await DbContext.Bookings.CountAsync();
        dbBookings.Should().Be(0);
    }

    [Fact]
    public async Task CreateBooking_WhenPromoCodeIsInvalid_ShouldThrowBusinessRuleException()
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

        var validLock = new SelectedSeat(session.Id, seat.Id, userId, DateTime.UtcNow.AddMinutes(10));
        DbContext.SelectedSeats.Add(validLock);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var request = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seat.Id },
            PromoCode = "INVALID_CODE"
        };

        SeatLockServiceMock
            .Setup(s => s.GetLockedSeatIdsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { seat.Id });

        var act = async () => await Mediator.Send(request);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("The promo code provided is invalid, expired, or has reached its usage limit.");

        var dbBookings = await DbContext.Bookings.CountAsync();
        dbBookings.Should().Be(0);
    }

    [Fact]
    public async Task CreateBooking_WhenSeatIsInactive_ShouldThrowBusinessRuleException()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Main Hall", 10, 10);
        DbContext.Halls.Add(hall);

        var seat = new Seat(hall.Id, "1", 1, SeatType.Standard, SeatStatus.Inactive);
        DbContext.Seats.Add(seat);

        var movie = new Movie(12345, "Inception", "Inception", "A thief...", 120, "poster.jpg", "backdrop.jpg", "Sci-Fi", "url", "PG-13", 8.8, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var validLock = new SelectedSeat(session.Id, seat.Id, userId, DateTime.UtcNow.AddMinutes(10));
        DbContext.SelectedSeats.Add(validLock);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var request = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seat.Id },
            PromoCode = null
        };

        SeatLockServiceMock
            .Setup(s => s.GetLockedSeatIdsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { seat.Id });

        var act = async () => await Mediator.Send(request);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("One or more selected seats are undergoing technical maintenance and cannot be purchased.");

        var dbBookings = await DbContext.Bookings.CountAsync();
        dbBookings.Should().Be(0);
    }
}
