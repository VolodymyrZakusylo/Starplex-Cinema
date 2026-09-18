using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Helpers;
using StarPlex.Application.Features.Bookings.Commands.CancelCashierBooking;
using StarPlex.Application.Features.Bookings.Commands.CreateCashierSale;
using StarPlex.Application.Features.Bookings.Commands.ScanTicket;
using StarPlex.Application.Features.Bookings.Queries.GetCashierRecentSales;
using StarPlex.Application.Features.Bookings.Queries.GetUserBookings;
using StarPlex.Application.Features.Halls.Commands.UpdateSeatProperties;
using StarPlex.Application.Features.Sessions.Commands.GenerateSchedule;
using StarPlex.Application.Features.Sessions.Commands.MoveSession;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.IntegrationTests.Features.Authorization;

// Real PostgreSQL and MediatR; only external services and the authenticated caller are mocked.
public class CinemaScopeTests : IntegrationTestBase
{
    public CinemaScopeTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private readonly Guid _callerId = Guid.NewGuid();

    private sealed record Fixtures(Cinema A, Cinema B, Hall HallA, Hall HallA2, Hall HallB,
        Movie Movie, Session SessionA, Session SessionB, Seat SeatA, Seat SeatB,
        Booking BookingA, Booking BookingB, Ticket TicketA, Ticket TicketB);

    private void Caller(string role, Guid? cinemaId)
    {
        CurrentUserServiceMock.SetupGet(u => u.UserId).Returns(_callerId);
        CurrentUserServiceMock.SetupGet(u => u.Role).Returns(role);
        CurrentUserServiceMock.SetupGet(u => u.IsSuperAdmin).Returns(role == "SuperAdmin");
        CurrentUserServiceMock.SetupGet(u => u.CinemaId).Returns(cinemaId);
    }

    private static DateTime FutureNoon => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelpers.KyivTimeZone)
            .Date.AddDays(2).AddHours(12), DateTimeKind.Unspecified), TimeZoneHelpers.KyivTimeZone);

    private async Task<Fixtures> Seed(bool imminent = false, bool customerOwned = false)
    {
        var a = new Cinema("Scope A", "Address A", "Kyiv");
        var b = new Cinema("Scope B", "Address B", "Kyiv");
        var ha = new Hall(a.Id, "A1", 2, 2);
        var ha2 = new Hall(a.Id, "A2", 2, 2);
        var hb = new Hall(b.Id, "B1", 2, 2);
        var sa = new Seat(ha.Id, "1", 1, SeatType.Standard);
        var sb = new Seat(hb.Id, "1", 1, SeatType.Standard);
        var movie = new Movie(9001, "Scope Movie", "Scope Movie", "Test", 90,
            "p.jpg", "b.jpg", "Drama", "url", "PG", 8, MovieStatus.NowShowing, DateTime.UtcNow);
        var start = imminent ? DateTime.UtcNow.AddMinutes(5) : FutureNoon;
        var sessionA = new Session(movie.Id, ha.Id, start, 90, 150, 150, SessionStatus.Active);
        var sessionB = new Session(movie.Id, hb.Id, start, 90, 150, 150, SessionStatus.Active);
        var ba = new Booking(customerOwned ? _callerId : Guid.Empty, sessionA.Id, 150, DateTime.UtcNow, BookingStatus.Confirmed);
        var bb = new Booking(customerOwned ? _callerId : Guid.Empty, sessionB.Id, 150, DateTime.UtcNow, BookingStatus.Confirmed);
        var bsa = new BookingSeat(ba.Id, sa.Id, 150m);
        var bsb = new BookingSeat(bb.Id, sb.Id, 150m);
        ba.BookingSeats.Add(bsa);
        bb.BookingSeats.Add(bsb);
        var ta = new Ticket(bsa.Id, "QA-SCOPE-A");
        var tb = new Ticket(bsb.Id, "QA-SCOPE-B");
        DbContext.Cinemas.AddRange(a, b);
        DbContext.Halls.AddRange(ha, ha2, hb);
        DbContext.Seats.AddRange(sa, sb);
        DbContext.Movies.Add(movie);
        DbContext.Sessions.AddRange(sessionA, sessionB);
        DbContext.Bookings.AddRange(ba, bb);
        DbContext.Payments.AddRange(new Payment(ba.Id, "POS-CASH-A", 150, PaymentStatus.Succeeded),
            new Payment(bb.Id, "POS-CASH-B", 150, PaymentStatus.Succeeded));
        DbContext.Tickets.AddRange(ta, tb);
        await DbContext.SaveChangesAsync(default);
        ((DbContext)DbContext).ChangeTracker.Clear();
        return new Fixtures(a, b, ha, ha2, hb, movie, sessionA, sessionB, sa, sb, ba, bb, ta, tb);
    }

    private async Task<string> Snapshot()
    {
        // Clear tracking so assertions see committed database state, not just tracked objects.
        ((DbContext)DbContext).ChangeTracker.Clear();
        return JsonSerializer.Serialize(new
        {
            Sessions = await DbContext.Sessions.OrderBy(s => s.Id)
                .Select(s => new { s.Id, s.HallId, s.StartTime, s.BasePrice, s.OriginalPrice }).ToListAsync(),
            Seats = await DbContext.Seats.OrderBy(s => s.Id).Select(s => new { s.Id, s.Type, s.Status }).ToListAsync(),
            Bookings = await DbContext.Bookings.OrderBy(b => b.Id).Select(b => new { b.Id, b.Status, b.TotalPrice }).ToListAsync(),
            Payments = await DbContext.Payments.OrderBy(p => p.Id).Select(p => new { p.Id, p.Status, p.PaidAt }).ToListAsync(),
            Tickets = await DbContext.Tickets.OrderBy(t => t.Id).Select(t => new { t.Id, t.IsUsed }).ToListAsync(),
            BookingSeats = await DbContext.BookingSeats.CountAsync(),
            Locks = await DbContext.SelectedSeats.CountAsync(),
            AuditLogs = await DbContext.AuditLogs.CountAsync()
        });
    }

    public static IEnumerable<object[]> OperationCases()
    {
        foreach (var operation in new[] { "Generate", "Sale", "Cancel", "Move", "Seat", "Scan" })
        {
            var roles = operation is "Sale" or "Cancel" or "Scan"
                ? new[] { "CinemaManager", "Cashier" } : new[] { "CinemaManager" };
            foreach (var role in roles)
                foreach (var scope in new[] { "same", "other", "missing" })
                    yield return new object[] { operation, role, scope };
            yield return new object[] { operation, "SuperAdmin", "missing" };
        }
    }

    [Theory]
    [MemberData(nameof(OperationCases))]
    public async Task Staff_operations_enforce_scope_before_mutation(string operation, string role, string scope)
    {
        var f = await Seed(imminent: operation == "Scan");
        Caller(role, scope == "same" ? f.A.Id : scope == "other" ? f.B.Id : null);
        if (operation == "Move")
        {
            // Remove the fixture's booking through the test DB so normal move rules permit the positive case.
            DbContext.Bookings.Remove(await DbContext.Bookings.SingleAsync(b => b.Id == f.BookingA.Id));
            await DbContext.SaveChangesAsync(default);
        }
        var before = await Snapshot();
        async Task Execute()
        {
            switch (operation)
            {
                case "Generate":
                    var count = await Mediator.Send(new GenerateScheduleCommand
                    {
                        CinemaId = f.A.Id, TargetDate = FutureNoon.AddDays(1), BasePrice = 150,
                        MovieIds = new() { f.Movie.Id }
                    });
                    count.Should().BeGreaterThan(0);
                    break;
                case "Sale":
                    // Use a new unsold seat; this suite isolates authorization from inventory validation.
                    var available = new Seat(f.HallA.Id, "2", 2, SeatType.Standard);
                    // Seed this seat before capturing the no-mutation snapshot below instead.
                    var seat = await DbContext.Seats.SingleAsync(s => s.HallId == f.HallA.Id && s.Number == 2);
                    var id = await Mediator.Send(new CreateCashierSaleCommand
                    { SessionId = f.SessionA.Id, SeatIds = new() { seat.Id }, PaymentMethod = "Cash" });
                    (await DbContext.Bookings.AsNoTracking().SingleAsync(b => b.Id == id)).Status.Should().Be(BookingStatus.Confirmed);
                    break;
                case "Cancel":
                    (await Mediator.Send(new CancelCashierBookingCommand { BookingId = f.BookingA.Id })).Should().BeTrue();
                    (await DbContext.Bookings.AsNoTracking().SingleAsync(b => b.Id == f.BookingA.Id)).Status.Should().Be(BookingStatus.Cancelled);
                    break;
                case "Move":
                    await Mediator.Send(new MoveSessionCommand { SessionId = f.SessionA.Id, HallId = f.HallA2.Id, NewStartTime = FutureNoon.AddHours(2) });
                    (await DbContext.Sessions.AsNoTracking().SingleAsync(s => s.Id == f.SessionA.Id)).HallId.Should().Be(f.HallA2.Id);
                    break;
                case "Seat":
                    await Mediator.Send(new UpdateSeatPropertiesCommand { SeatId = f.SeatA.Id, Type = SeatType.VIP, Status = SeatStatus.Active });
                    (await DbContext.Seats.AsNoTracking().SingleAsync(s => s.Id == f.SeatA.Id)).Type.Should().Be(SeatType.VIP);
                    break;
                case "Scan":
                    (await Mediator.Send(new ScanTicketCommand { TicketCode = f.TicketA.TicketCode })).IsSuccess.Should().BeTrue();
                    (await DbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == f.TicketA.Id)).IsUsed.Should().BeTrue();
                    break;
            }
        }
        if (operation == "Sale")
        {
            var saleSeat = new Seat(f.HallA.Id, "2", 2, SeatType.Standard);
            DbContext.Seats.Add(saleSeat);
            DbContext.SelectedSeats.Add(new SelectedSeat(f.SessionA.Id, saleSeat.Id, _callerId, DateTime.UtcNow.AddMinutes(10)));
            await DbContext.SaveChangesAsync(default);
            before = await Snapshot();
        }
        if (role == "SuperAdmin" || scope == "same")
        {
            await Execute();
        }
        else
        {
            Func<Task> action = Execute;
            await action.Should().ThrowAsync<ForbiddenException>();
            (await Snapshot()).Should().Be(before, "denial must not create or mutate bookings, payments, tickets, seats or sessions");
            PaymentServiceMock.Invocations.Should().BeEmpty();
            SeatHubServiceMock.Invocations.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Move_checks_both_source_and_target_cinemas(bool superAdmin, bool sourceOutside)
    {
        var f = await Seed();
        Caller(superAdmin ? "SuperAdmin" : "CinemaManager", f.A.Id);
        var source = sourceOutside ? f.SessionB : f.SessionA;
        DbContext.Bookings.RemoveRange(DbContext.Bookings);
        await DbContext.SaveChangesAsync(default);
        var before = await Snapshot();
        var target = sourceOutside ? f.HallA2 : f.HallB;
        Func<Task> action = async () => await Mediator.Send(new MoveSessionCommand
        { SessionId = source.Id, HallId = target.Id, NewStartTime = FutureNoon.AddHours(2) });
        if (superAdmin)
        {
            await action();
            (await DbContext.Sessions.AsNoTracking().SingleAsync(s => s.Id == source.Id)).HallId.Should().Be(target.Id);
        }
        else
        {
            await action.Should().ThrowAsync<ForbiddenException>();
            (await Snapshot()).Should().Be(before);
        }
    }

    [Theory]
    [InlineData("Cashier", false)]
    [InlineData("CinemaManager", false)]
    [InlineData("Cashier", true)]
    [InlineData("CinemaManager", true)]
    [InlineData("SuperAdmin", true)]
    public async Task History_filters_by_assigned_cinema_and_denies_missing_assignment(string role, bool missing)
    {
        var f = await Seed();
        Caller(role, missing ? null : f.A.Id);
        if (missing && role != "SuperAdmin")
        {
            Func<Task> action = async () => await Mediator.Send(new GetCashierRecentSalesQuery());
            await action.Should().ThrowAsync<ForbiddenException>();
            return;
        }
        var sales = await Mediator.Send(new GetCashierRecentSalesQuery());
        sales.Select(s => s.BookingId).Should().BeEquivalentTo(role == "SuperAdmin"
            ? new[] { f.BookingA.Id, f.BookingB.Id } : new[] { f.BookingA.Id });
    }

    [Fact]
    public async Task Customer_history_remains_owner_scoped_across_cinemas()
    {
        var f = await Seed(customerOwned: true);
        Caller("Customer", null);
        var bookings = await Mediator.Send(new GetUserBookingsQuery { UserId = _callerId });
        bookings.Select(b => b.Id).Should().BeEquivalentTo(new[] { f.BookingA.Id, f.BookingB.Id });
    }
}
