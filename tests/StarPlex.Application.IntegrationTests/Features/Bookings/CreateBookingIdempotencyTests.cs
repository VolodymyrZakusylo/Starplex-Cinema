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

public class CreateBookingIdempotencyTests : IntegrationTestBase
{
    public CreateBookingIdempotencyTests(DatabaseFixture fixture) : base(fixture)
    {
    }
    private async Task<(Cinema Cinema, Hall Hall, Session Session, List<Seat> Seats)> SeedCinemaSessionWithSeats(int seatCount = 3)
    {
        var cinema = new Cinema("Test Cinema", "Test Address", "Kyiv");
        var hall = new Hall(cinema.Id, "Hall 1", 1, seatCount);
        var movie = new Movie(3001, "Test Movie", "Test Movie", "Test", 120, "p.jpg", "b.jpg", "Action", "url", "PG", 10, MovieStatus.NowShowing, DateTime.UtcNow);
        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddHours(5), 120, 200, 200, SessionStatus.Active);

        DbContext.Cinemas.Add(cinema);
        DbContext.Halls.Add(hall);
        DbContext.Movies.Add(movie);
        DbContext.Sessions.Add(session);

        var seats = new List<Seat>();
        for (int i = 1; i <= seatCount; i++)
        {
            var seat = new Seat(hall.Id, "1", i, SeatType.Standard);
            seats.Add(seat);
            DbContext.Seats.Add(seat);
        }

        await DbContext.SaveChangesAsync(default);
        return (cinema, hall, session, seats);
    }

    private async Task SeedSelectedSeatLocks(Guid sessionId, IEnumerable<Guid> seatIds, Guid userId, int minutesValid = 10)
    {
        var expiry = DateTime.UtcNow.AddMinutes(minutesValid);
        foreach (var seatId in seatIds)
        {
            DbContext.SelectedSeats.Add(new SelectedSeat(sessionId, seatId, userId, expiry));
        }
        await DbContext.SaveChangesAsync(default);
    }

    [Fact]
    public async Task CreateBooking_ValidOnlineBooking_CreatesOneBookingPaymentAndStripeCall()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        var userId = Guid.NewGuid();
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, userId);

        PaymentServiceMock
            .Setup(p => p.CreatePaymentIntentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), "uah", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("pi_test_123_secret_xyz");

        var command = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id }
        };

        var response = await Mediator.Send(command);

        response.Should().NotBeNull();
        response.BookingId.Should().NotBeEmpty();
        response.ClientSecret.Should().Be("pi_test_123_secret_xyz");

        var booking = await DbContext.Bookings
            .Include(b => b.Payment)
            .Include(b => b.BookingSeats)
            .FirstOrDefaultAsync(b => b.Id == response.BookingId);

        booking.Should().NotBeNull();
        booking!.Status.Should().Be(BookingStatus.Pending);
        booking.Payment.Should().NotBeNull();
        booking.Payment!.StripePaymentIntentId.Should().Be("pi_test_123");

        PaymentServiceMock.Verify(p => p.CreatePaymentIntentAsync(
            booking.Id,
            booking.TotalPrice,
            "uah",
            booking.Id.ToString(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBooking_SequentialDuplicateRequest_ReusesExistingPendingBookingAndPaymentIntent()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        var userId = Guid.NewGuid();
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, userId);

        PaymentServiceMock
            .Setup(p => p.CreatePaymentIntentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), "uah", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("pi_test_123_secret_xyz");

        var command = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id }
        };

        var response1 = await Mediator.Send(command);
        var response2 = await Mediator.Send(command);

        response2.BookingId.Should().Be(response1.BookingId);
        response2.ClientSecret.Should().Be(response1.ClientSecret);

        var bookingsCount = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        bookingsCount.Should().Be(1);

        var paymentsCount = await DbContext.Payments.CountAsync();
        paymentsCount.Should().Be(1);

        PaymentServiceMock.Verify(p => p.CreatePaymentIntentAsync(
            response1.BookingId,
            It.IsAny<decimal>(),
            "uah",
            response1.BookingId.ToString(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateBooking_ConcurrentDuplicateRequests_OnlyOneBookingAndPaymentCreated()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        var userId = Guid.NewGuid();
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, userId);

        PaymentServiceMock
            .Setup(p => p.CreatePaymentIntentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), "uah", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("pi_test_concurrent_secret_xyz");

        var command1 = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id }
        };

        var command2 = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id }
        };

        var task1 = Task.Run(async () =>
        {
            using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
            var mediator = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MediatR.IMediator>(scope.ServiceProvider);
            return await mediator.Send(command1);
        });

        var task2 = Task.Run(async () =>
        {
            using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
            var mediator = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MediatR.IMediator>(scope.ServiceProvider);
            return await mediator.Send(command2);
        });

        var results = await Task.WhenAll(task1, task2);

        results[0].BookingId.Should().Be(results[1].BookingId);
        results[0].ClientSecret.Should().Be(results[1].ClientSecret);

        var totalBookings = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        totalBookings.Should().Be(1);

        var totalPayments = await DbContext.Payments.CountAsync();
        totalPayments.Should().Be(1);
    }

    [Fact]
    public async Task CreateBooking_SameCustomerDifferentFreeSeatSet_CreatesSeparateBooking()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(2);
        var userId = Guid.NewGuid();
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id, seats[1].Id }, userId);

        PaymentServiceMock
            .Setup(p => p.CreatePaymentIntentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), "uah", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid bId, decimal a, string c, string? k, CancellationToken ct) => $"pi_{bId}_secret_xyz");

        var command1 = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id }
        };

        var command2 = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[1].Id }
        };

        var res1 = await Mediator.Send(command1);
        var res2 = await Mediator.Send(command2);

        res1.BookingId.Should().NotBe(res2.BookingId);

        var totalBookings = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        totalBookings.Should().Be(2);
    }

    [Fact]
    public async Task CreateBooking_SameCustomerOverlappingSeatSet_Rejects()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(2);
        var userId = Guid.NewGuid();
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id, seats[1].Id }, userId);

        PaymentServiceMock
            .Setup(p => p.CreatePaymentIntentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), "uah", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("pi_test_123_secret_xyz");

        var command1 = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id }
        };

        var res1 = await Mediator.Send(command1);
        res1.BookingId.Should().NotBeEmpty();

        var command2 = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id, seats[1].Id }
        };

        Func<Task> action = async () => await Mediator.Send(command2);
        await action.Should().ThrowAsync<BusinessRuleException>();

        var totalBookings = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        totalBookings.Should().Be(1);
    }

    [Fact]
    public async Task CreateBooking_SeatAlreadyPendingOrConfirmedForAnotherUser_Rejects()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, userA);

        var existingBooking = new Booking(userA, session.Id, 200, DateTime.UtcNow, BookingStatus.Pending);
        existingBooking.BookingSeats.Add(new BookingSeat(existingBooking.Id, seats[0].Id, 200m));
        DbContext.Bookings.Add(existingBooking);
        await DbContext.SaveChangesAsync(default);

        var lockA = await DbContext.SelectedSeats.FirstAsync(ss => ss.SessionId == session.Id && ss.SeatId == seats[0].Id);
        DbContext.SelectedSeats.Remove(lockA);
        await DbContext.SaveChangesAsync(default);

        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, userB);

        var commandUserB = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userB,
            SeatIds = new List<Guid> { seats[0].Id }
        };

        Func<Task> action = async () => await Mediator.Send(commandUserB);
        await action.Should().ThrowAsync<BusinessRuleException>();

        PaymentServiceMock.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateBooking_MultiSeatWithOneSeatUnavailable_RejectsEntireBooking()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(3);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var confirmedBooking = new Booking(userA, session.Id, 200, DateTime.UtcNow, BookingStatus.Confirmed);
        confirmedBooking.BookingSeats.Add(new BookingSeat(confirmedBooking.Id, seats[1].Id, 200m));
        DbContext.Bookings.Add(confirmedBooking);
        await DbContext.SaveChangesAsync(default);

        await SeedSelectedSeatLocks(session.Id, seats.Select(s => s.Id), userB);

        var commandUserB = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userB,
            SeatIds = new List<Guid> { seats[0].Id, seats[1].Id, seats[2].Id }
        };

        Func<Task> action = async () => await Mediator.Send(commandUserB);
        await action.Should().ThrowAsync<BusinessRuleException>();

        var userBBookings = await DbContext.Bookings.CountAsync(b => b.UserId == userB);
        userBBookings.Should().Be(0);
    }

    [Fact]
    public async Task CreateBooking_CallerLacksLockOnOneSeat_RejectsWithoutStripeCall()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(2);
        var userId = Guid.NewGuid();
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, userId);

        var command = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id, seats[1].Id }
        };

        Func<Task> action = async () => await Mediator.Send(command);
        await action.Should().ThrowAsync<BusinessRuleException>();

        PaymentServiceMock.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateBooking_ExpiredLock_RejectsWithoutStripeCall()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        var userId = Guid.NewGuid();
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, userId, minutesValid: -5);

        var command = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id }
        };

        Func<Task> action = async () => await Mediator.Send(command);
        await action.Should().ThrowAsync<BusinessRuleException>();

        PaymentServiceMock.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateBooking_WhenPaymentIntentIdWasNotPersisted_RetryUsesSameIdempotencyKey()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        var userId = Guid.NewGuid();
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, userId);

        PaymentServiceMock
            .Setup(p => p.CreatePaymentIntentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), "uah", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("pi_recovered_123_secret_xyz");

        var command = new CreateBookingCommand
        {
            SessionId = session.Id,
            UserId = userId,
            SeatIds = new List<Guid> { seats[0].Id }
        };

        var response1 = await Mediator.Send(command);
        response1.ClientSecret.Should().Be("pi_recovered_123_secret_xyz");

        var payment = await DbContext.Payments.FirstAsync(p => p.BookingId == response1.BookingId);
        payment.StripePaymentIntentId = string.Empty;
        await DbContext.SaveChangesAsync(default);

        var response2 = await Mediator.Send(command);

        response2.BookingId.Should().Be(response1.BookingId);
        response2.ClientSecret.Should().Be("pi_recovered_123_secret_xyz");

        PaymentServiceMock.Verify(p => p.CreatePaymentIntentAsync(
            response1.BookingId,
            It.IsAny<decimal>(),
            "uah",
            response1.BookingId.ToString(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        var updatedPayment = await DbContext.Payments.FirstAsync(p => p.BookingId == response1.BookingId);
        updatedPayment.StripePaymentIntentId.Should().Be("pi_recovered_123");
    }
}
