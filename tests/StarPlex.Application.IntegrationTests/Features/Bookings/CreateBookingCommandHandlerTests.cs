using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarPlex.Application.Features.Bookings.Commands.CreateBooking;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class CreateBookingCommandHandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateBooking_ShouldCreateBookingSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        // Setup minimal DB state
        var cinema = new Cinema("StarPlex", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Main Hall", 10, 10);
        DbContext.Halls.Add(hall);
        
        var seat1 = new Seat(hall.Id, "A", 1, SeatType.Standard);
        var seat2 = new Seat(hall.Id, "A", 2, SeatType.Standard);
        DbContext.Seats.AddRange(seat1, seat2);

        var movie = new Movie(12345, "Inception", "Inception", "A thief who steals corporate secrets...", 120, "poster.jpg", "backdrop.jpg", "Sci-Fi", "url", "PG-13", 8.8, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        // Setup SelectedSeats to pass lock validation
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

        // Setup mock for ISeatLockService
        SeatLockServiceMock
            .Setup(s => s.GetLockedSeatIdsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { seat1.Id, seat2.Id });

        // Setup mock for IPaymentService
        PaymentServiceMock
            .Setup(p => p.CreatePaymentIntentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync("pi_test_123");

        // Act
        var result = await Mediator.Send(request);

        // Assert
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
}
