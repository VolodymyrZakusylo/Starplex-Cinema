using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Features.Cinemas.Commands.DeleteCinema;
using StarPlex.Application.Features.Discounts.Commands.DeletePromoCode;
using StarPlex.Application.Features.Halls.Commands.DeleteHall;
using StarPlex.Application.Features.Sessions.Commands.CreateSession;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Infrastructure.Identity;
using StarPlex.Infrastructure.Persistence;

namespace StarPlex.Application.IntegrationTests.Features;

public class AdminCrudTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;
    public AdminCrudTests(DatabaseFixture fixture) : base(fixture) => _fixture = fixture;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NeverUsedHallOrCinema_CanBeDeleted(bool cinema)
    {
        var hall = await SeedHallAsync();
        if (cinema) await Mediator.Send(new DeleteCinemaCommand { Id = hall.CinemaId });
        else await Mediator.Send(new DeleteHallCommand { Id = hall.Id });
        (await DbContext.Halls.AnyAsync()).Should().BeFalse();
        (await DbContext.Seats.AnyAsync()).Should().BeFalse();
        (await DbContext.Cinemas.AnyAsync()).Should().Be(!cinema);
    }

    [Theory]
    [InlineData(false, SessionStatus.Cancelled)]
    [InlineData(false, SessionStatus.Completed)]
    [InlineData(true, SessionStatus.Cancelled)]
    [InlineData(true, SessionStatus.Completed)]
    public async Task HistoricalSessions_PreventHallAndCinemaDeletion(bool cinema, SessionStatus status)
    {
        var hall = await SeedHallAsync();
        var session = await SeedSessionAsync(hall.Id, status);
        var booking = new Booking(Guid.NewGuid(), session.Id, 150, DateTime.UtcNow, BookingStatus.Confirmed);
        var seat = await DbContext.Seats.SingleAsync();
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150);
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);
        DbContext.Tickets.Add(new Ticket(bookingSeat.Id, "historic-ticket"));
        DbContext.Payments.Add(new Payment(booking.Id, "pi_history", 150, PaymentStatus.Succeeded));
        await DbContext.SaveChangesAsync(default);

        Func<Task> delete = async () =>
        {
            if (cinema) await Mediator.Send(new DeleteCinemaCommand { Id = hall.CinemaId });
            else await Mediator.Send(new DeleteHallCommand { Id = hall.Id });
        };
        await delete.Should().ThrowAsync<ConflictException>().WithMessage("*historical sessions*");
        (await DbContext.Sessions.CountAsync()).Should().Be(1);
        (await DbContext.Bookings.CountAsync()).Should().Be(1);
        (await DbContext.Tickets.CountAsync()).Should().Be(1);
        (await DbContext.Payments.CountAsync()).Should().Be(1);
        (await DbContext.Halls.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task InactiveHall_RejectsManualSession()
    {
        var hall = await SeedHallAsync();
        hall.IsActive = false;
        await DbContext.SaveChangesAsync(default);
        var create = () => Mediator.Send(new CreateSessionCommand { HallId = hall.Id, MovieId = Guid.NewGuid(), StartTime = DateTime.UtcNow.AddDays(1), BasePrice = 150 });
        await create.Should().ThrowAsync<BusinessRuleException>().WithMessage("*inactive hall*");
        (await DbContext.Sessions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AssignedStaff_PreventsCinemaDeletion()
    {
        var hall = await SeedHallAsync();
        var db = (ApplicationDbContext)DbContext;
        db.Users.Add(new ApplicationUser { UserName = "assigned", CinemaId = hall.CinemaId });
        await db.SaveChangesAsync();
        var delete = () => Mediator.Send(new DeleteCinemaCommand { Id = hall.CinemaId });
        await delete.Should().ThrowAsync<ConflictException>().WithMessage("*staff*");
        (await DbContext.Cinemas.AnyAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task UnreferencedPromo_IsPhysicallyDeleted_EvenWithUsageCount()
    {
        var discount = await SeedDiscountAsync(expired: true, usageCount: 50);
        var result = await Mediator.Send(new DeletePromoCodeCommand(discount.Id));
        result.Outcome.Should().Be("Deleted");
        (await DbContext.Discounts.AnyAsync()).Should().BeFalse();
    }

    [Theory]
    [InlineData(BookingStatus.Pending, false, 0)]
    [InlineData(BookingStatus.Confirmed, true, 10)]
    [InlineData(BookingStatus.Cancelled, true, 0)]
    public async Task ReferencedPromo_IsDeactivated_PreservingHistory(BookingStatus status, bool expired, int usageCount)
    {
        var hall = await SeedHallAsync();
        var session = await SeedSessionAsync(hall.Id, SessionStatus.Completed);
        var discount = await SeedDiscountAsync(expired, usageCount);
        DbContext.Bookings.Add(new Booking(Guid.NewGuid(), session.Id, 150, DateTime.UtcNow, status) { DiscountId = discount.Id });
        await DbContext.SaveChangesAsync(default);

        var result = await Mediator.Send(new DeletePromoCodeCommand(discount.Id));

        result.Outcome.Should().Be("Deactivated");
        (await DbContext.Discounts.AsNoTracking().SingleAsync()).IsActive.Should().BeFalse();
        (await DbContext.Bookings.AsNoTracking().SingleAsync()).DiscountId.Should().Be(discount.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SessionInsertedAfterDeleteValidation_IsControlledConflict(bool cinema)
    {
        var hall = await SeedHallAsync();
        var interceptor = new BeforeSave(async () => { await SeedSessionAsync(hall.Id, SessionStatus.Cancelled); });
        await using var deletingDb = NewContext(interceptor);
        Func<Task> delete = async () =>
        {
            if (cinema) await new DeleteCinemaCommandHandler(deletingDb).Handle(new DeleteCinemaCommand { Id = hall.CinemaId }, default);
            else await new DeleteHallCommandHandler(deletingDb, CurrentUserServiceMock.Object).Handle(new DeleteHallCommand { Id = hall.Id }, default);
        };
        await delete.Should().ThrowAsync<ConflictException>();
        (await DbContext.Sessions.CountAsync()).Should().Be(1);
        (await DbContext.Halls.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task BookingInsertedAfterPromoDeleteValidation_FallsBackToDeactivation()
    {
        var hall = await SeedHallAsync();
        var session = await SeedSessionAsync(hall.Id, SessionStatus.Active);
        var discount = await SeedDiscountAsync(false, 0);
        var interceptor = new BeforeSave(async () =>
        {
            DbContext.Bookings.Add(new Booking(Guid.NewGuid(), session.Id, 150, DateTime.UtcNow, BookingStatus.Pending) { DiscountId = discount.Id });
            await DbContext.SaveChangesAsync(default);
        });
        await using var deletingDb = NewContext(interceptor);

        var result = await new DeletePromoCodeCommandHandler(deletingDb).Handle(new DeletePromoCodeCommand(discount.Id), default);

        result.Outcome.Should().Be("Deactivated");
        (await DbContext.Discounts.AsNoTracking().SingleAsync()).IsActive.Should().BeFalse();
        (await DbContext.Bookings.AsNoTracking().SingleAsync()).DiscountId.Should().Be(discount.Id);
    }

    private ApplicationDbContext NewContext(IInterceptor interceptor) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_fixture.ConnectionString).AddInterceptors(interceptor).Options,
        CurrentUserServiceMock.Object);

    private async Task<Hall> SeedHallAsync()
    {
        CurrentUserServiceMock.SetupGet(u => u.IsSuperAdmin).Returns(true);
        var cinema = new Cinema("CRUD", "Test", "Kyiv");
        var hall = new Hall(cinema.Id, "Hall", 1, 1);
        DbContext.Cinemas.Add(cinema);
        DbContext.Halls.Add(hall);
        DbContext.Seats.Add(new Seat(hall.Id, "1", 1, SeatType.Standard));
        await DbContext.SaveChangesAsync(default);
        return hall;
    }

    private async Task<Session> SeedSessionAsync(Guid hallId, SessionStatus status)
    {
        var movie = new Movie(4444, "CRUD", "CRUD", "Desc", 90, "p", "b", "Drama", "u", "PG", 8, MovieStatus.NowShowing, DateTime.UtcNow);
        var session = new Session(movie.Id, hallId, DateTime.UtcNow.AddDays(-2), 90, 150, 150, status);
        DbContext.Movies.Add(movie);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(default);
        return session;
    }

    private async Task<Discount> SeedDiscountAsync(bool expired, int usageCount)
    {
        var discount = new Discount { Code = "CRUD", Name = "CRUD", IsActive = true, Percentage = 10, UsageCount = usageCount, UsageLimit = 100, ValidFrom = DateTime.UtcNow.AddDays(-5), ValidTo = DateTime.UtcNow.AddDays(expired ? -1 : 1) };
        DbContext.Discounts.Add(discount);
        await DbContext.SaveChangesAsync(default);
        return discount;
    }

    private sealed class BeforeSave(Func<Task> action) : SaveChangesInterceptor
    {
        private bool _ran;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_ran)
            {
                _ran = true;
                await action();
            }
            return result;
        }
    }
}
