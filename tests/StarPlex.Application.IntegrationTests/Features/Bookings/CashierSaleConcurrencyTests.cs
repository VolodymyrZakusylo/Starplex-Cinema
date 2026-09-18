using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Bookings.Commands.CreateCashierSale;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class CashierSaleConcurrencyTests : IntegrationTestBase
{
    public CashierSaleConcurrencyTests(DatabaseFixture fixture) : base(fixture)
    {
    }
    private readonly Guid _staffUserId = Guid.NewGuid();

    private void SetStaffUser(Guid cinemaId, bool isSuperAdmin = false)
    {
        CurrentUserServiceMock.SetupGet(u => u.UserId).Returns(_staffUserId);
        CurrentUserServiceMock.SetupGet(u => u.Role).Returns(isSuperAdmin ? "SuperAdmin" : "Cashier");
        CurrentUserServiceMock.SetupGet(u => u.IsSuperAdmin).Returns(isSuperAdmin);
        CurrentUserServiceMock.SetupGet(u => u.CinemaId).Returns(isSuperAdmin ? null : cinemaId);
    }

    private async Task<(Cinema Cinema, Hall Hall, Session Session, List<Seat> Seats)> SeedCinemaSessionWithSeats(int seatCount = 3)
    {
        var cinema = new Cinema("Test Cinema", "Test Address", "Kyiv");
        var hall = new Hall(cinema.Id, "Hall 1", 1, seatCount);
        var movie = new Movie(2001, "Concurrency Movie", "Concurrency Movie", "Test", 120, "p.jpg", "b.jpg", "Action", "url", "PG", 10, MovieStatus.NowShowing, DateTime.UtcNow);
        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddHours(3), 120, 200, 200, SessionStatus.Active);

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
    public async Task CashierSale_ValidSingleSeat_SucceedsAndCreatesBookingAndTickets()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, _staffUserId);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        var bookingId = await Mediator.Send(command);

        bookingId.Should().NotBeEmpty();

        var booking = await DbContext.Bookings
            .Include(b => b.BookingSeats)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        booking.Should().NotBeNull();
        booking!.Status.Should().Be(BookingStatus.Confirmed);
        booking.BookingSeats.Should().HaveCount(1);

        var tickets = await DbContext.Tickets.Where(t => t.BookingSeat.BookingId == bookingId).ToListAsync();
        tickets.Should().HaveCount(1);
    }

    [Fact]
    public async Task CashierSale_SequentialDuplicateRequest_RejectsSecondCallAndLeavesOnlyOneBooking()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, _staffUserId);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        var firstBookingId = await Mediator.Send(command);
        firstBookingId.Should().NotBeEmpty();

        Func<Task> secondCall = async () => await Mediator.Send(command);
        await secondCall.Should().ThrowAsync<BusinessRuleException>();

        var totalBookingsForSession = await DbContext.Bookings
            .Where(b => b.SessionId == session.Id && b.Status == BookingStatus.Confirmed)
            .CountAsync();

        totalBookingsForSession.Should().Be(1);

        var totalTicketsForSession = await DbContext.Tickets
            .Where(t => t.BookingSeat.Booking.SessionId == session.Id)
            .CountAsync();

        totalTicketsForSession.Should().Be(1);
    }

    [Fact]
    public async Task CashierSale_SeatAlreadySoldByAnotherBooking_RejectsCashierSale()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, _staffUserId);

        var existingBooking = new Booking(Guid.NewGuid(), session.Id, 200, DateTime.UtcNow, BookingStatus.Confirmed);
        var existingBookingSeat = new BookingSeat(existingBooking.Id, seats[0].Id, 200m);
        existingBooking.BookingSeats.Add(existingBookingSeat);
        DbContext.Bookings.Add(existingBooking);
        await DbContext.SaveChangesAsync(default);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        Func<Task> action = async () => await Mediator.Send(command);
        await action.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CashierSale_MultiSeatWhereOneIsAlreadySold_RejectsEntireSaleAndCreatesNoPartialBooking()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(3);
        SetStaffUser(cinema.Id);
        await SeedSelectedSeatLocks(session.Id, seats.Select(s => s.Id), _staffUserId);

        var existingBooking = new Booking(Guid.NewGuid(), session.Id, 200, DateTime.UtcNow, BookingStatus.Confirmed);
        var existingBookingSeat = new BookingSeat(existingBooking.Id, seats[1].Id, 200m);
        existingBooking.BookingSeats.Add(existingBookingSeat);
        DbContext.Bookings.Add(existingBooking);
        await DbContext.SaveChangesAsync(default);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id, seats[1].Id, seats[2].Id },
            PaymentMethod = "Cash"
        };

        Func<Task> action = async () => await Mediator.Send(command);
        await action.Should().ThrowAsync<BusinessRuleException>();

        var bookingsCount = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        bookingsCount.Should().Be(1);

        var soldSeatIds = await DbContext.BookingSeats
            .Where(bs => bs.Booking.SessionId == session.Id)
            .Select(bs => bs.SeatId)
            .ToListAsync();

        soldSeatIds.Should().ContainSingle().Which.Should().Be(seats[1].Id);
    }

    [Fact]
    public async Task CashierSale_ConcurrentRequestsForSameSeat_ExactlyOneSucceedsAndOneIsRejected()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, _staffUserId);

        var command1 = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        var command2 = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Card"
        };

        var task1 = Task.Run(async () =>
        {
            using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
            var mediator = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MediatR.IMediator>(scope.ServiceProvider);
            try
            {
                return (Success: true, BookingId: await mediator.Send(command1), Error: (Exception?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, BookingId: Guid.Empty, Error: ex);
            }
        });

        var task2 = Task.Run(async () =>
        {
            using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
            var mediator = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MediatR.IMediator>(scope.ServiceProvider);
            try
            {
                return (Success: true, BookingId: await mediator.Send(command2), Error: (Exception?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, BookingId: Guid.Empty, Error: ex);
            }
        });

        var results = await Task.WhenAll(task1, task2);

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        successCount.Should().Be(1);
        failureCount.Should().Be(1);

        results.First(r => !r.Success).Error.Should().BeOfType<BusinessRuleException>();

        var totalBookings = await DbContext.Bookings
            .Where(b => b.SessionId == session.Id && b.Status == BookingStatus.Confirmed)
            .CountAsync();
        totalBookings.Should().Be(1);

        var totalTickets = await DbContext.Tickets
            .Where(t => t.BookingSeat.Booking.SessionId == session.Id)
            .CountAsync();
        totalTickets.Should().Be(1);
    }

    [Fact]
    public async Task CashierSale_ReleasesTemporarySelectedSeatsLocks()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, _staffUserId);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        await Mediator.Send(command);

        var remainingLocks = await DbContext.SelectedSeats
            .Where(ss => ss.SessionId == session.Id && ss.SeatId == seats[0].Id)
            .CountAsync();

        remainingLocks.Should().Be(0);
    }

    [Fact]
    public async Task CashierSale_CashierOwnsLock_SaleSucceedsAndConsumesLock()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);
        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, _staffUserId);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        var bookingId = await Mediator.Send(command);
        bookingId.Should().NotBeEmpty();

        var lockExists = await DbContext.SelectedSeats
            .AnyAsync(ss => ss.SessionId == session.Id && ss.SeatId == seats[0].Id);
        lockExists.Should().BeFalse();
    }

    [Fact]
    public async Task CashierSale_AnotherCustomerOwnsActiveLock_SaleRejectedAndLockRemains()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);
        var customerId = Guid.NewGuid();

        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, customerId);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        Func<Task> action = async () => await Mediator.Send(command);
        await action.Should().ThrowAsync<BusinessRuleException>();

        var customerLockExists = await DbContext.SelectedSeats
            .AnyAsync(ss => ss.SessionId == session.Id && ss.SeatId == seats[0].Id && ss.UserId == customerId);
        customerLockExists.Should().BeTrue();

        var bookingsCount = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        bookingsCount.Should().Be(0);
    }

    [Fact]
    public async Task CashierSale_AnotherCashierOwnsActiveLock_SaleRejectedAndLockRemains()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);
        var otherCashierId = Guid.NewGuid();

        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, otherCashierId);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        Func<Task> action = async () => await Mediator.Send(command);
        await action.Should().ThrowAsync<BusinessRuleException>();

        var lockExists = await DbContext.SelectedSeats
            .AnyAsync(ss => ss.SessionId == session.Id && ss.SeatId == seats[0].Id && ss.UserId == otherCashierId);
        lockExists.Should().BeTrue();

        var bookingsCount = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        bookingsCount.Should().Be(0);
    }

    [Fact]
    public async Task CashierSale_OneOfSeveralSeatsLacksCallerLock_EntireSaleRejected()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(3);
        SetStaffUser(cinema.Id);

        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id, seats[1].Id }, _staffUserId);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id, seats[1].Id, seats[2].Id },
            PaymentMethod = "Cash"
        };

        Func<Task> action = async () => await Mediator.Send(command);
        await action.Should().ThrowAsync<BusinessRuleException>();

        var bookingsCount = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        bookingsCount.Should().Be(0);

        var remainingLocks = await DbContext.SelectedSeats
            .Where(ss => ss.SessionId == session.Id && ss.UserId == _staffUserId)
            .CountAsync();
        remainingLocks.Should().Be(2);
    }

    [Fact]
    public async Task CashierSale_ExpiredLock_SaleRejected()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);

        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, _staffUserId, minutesValid: -5);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        Func<Task> action = async () => await Mediator.Send(command);
        await action.Should().ThrowAsync<BusinessRuleException>();

        var bookingsCount = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        bookingsCount.Should().Be(0);
    }

    [Fact]
    public async Task CashierSale_SuccessfulSaleRemovesOnlyCallerOwnedLocks()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(2);
        SetStaffUser(cinema.Id);
        var otherUserId = Guid.NewGuid();

        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, _staffUserId);
        await SeedSelectedSeatLocks(session.Id, new[] { seats[1].Id }, otherUserId);

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        var bookingId = await Mediator.Send(command);
        bookingId.Should().NotBeEmpty();

        var callerLockExists = await DbContext.SelectedSeats
            .AnyAsync(ss => ss.SessionId == session.Id && ss.SeatId == seats[0].Id && ss.UserId == _staffUserId);
        callerLockExists.Should().BeFalse();

        var otherLockExists = await DbContext.SelectedSeats
            .AnyAsync(ss => ss.SessionId == session.Id && ss.SeatId == seats[1].Id && ss.UserId == otherUserId);
        otherLockExists.Should().BeTrue();
    }

    [Fact]
    public async Task CashierSale_StaleSeatLockExpirationDuringLockContention_RejectsExpiredLockPostAcquisition()
    {
        var (cinema, hall, session, seats) = await SeedCinemaSessionWithSeats(1);
        SetStaffUser(cinema.Id);

        await SeedSelectedSeatLocks(session.Id, new[] { seats[0].Id }, _staffUserId, minutesValid: 0);
        var seatLock = await DbContext.SelectedSeats.FirstAsync(ss => ss.SessionId == session.Id && ss.SeatId == seats[0].Id);
        seatLock.LockedUntil = DateTime.UtcNow.AddMilliseconds(500);
        await DbContext.SaveChangesAsync(default);

        using var holderScope = ServiceProvider.CreateScope();
        var holderContext = holderScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var holderDb = (DbContext)holderContext;
        await using var holderTx = await holderDb.Database.BeginTransactionAsync();
        await holderDb.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Sessions\" WHERE \"Id\" = {session.Id} FOR UPDATE");

        var command = new CreateCashierSaleCommand
        {
            SessionId = session.Id,
            SeatIds = new List<Guid> { seats[0].Id },
            PaymentMethod = "Cash"
        };

        var sendTask = Task.Run(async () =>
        {
            using var scope = ServiceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
            return await mediator.Send(command);
        });

        await Task.Delay(800);
        await holderTx.RollbackAsync();

        Func<Task> action = async () => await sendTask;
        await action.Should().ThrowAsync<BusinessRuleException>();

        var totalBookings = await DbContext.Bookings.CountAsync(b => b.SessionId == session.Id);
        totalBookings.Should().Be(0);

        var totalTickets = await DbContext.Tickets.CountAsync(t => t.BookingSeat.Booking.SessionId == session.Id);
        totalTickets.Should().Be(0);
    }
}
