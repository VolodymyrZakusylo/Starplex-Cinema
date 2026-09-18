using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Features.Bookings.Commands.CancelTicket;
using StarPlex.Application.Features.Discounts.Commands.ApplyPromoCode;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class CancelTicketCommandHandlerTests : IntegrationTestBase
{
    public CancelTicketCommandHandlerTests(DatabaseFixture fixture) : base(fixture)
    {
    }

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
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_test", 150m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket = new Ticket(bookingSeat.Id, "CODE123");
        DbContext.Tickets.Add(ticket);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_test", 150m, "uah", It.IsAny<CancellationToken>(), It.IsAny<string?>()))
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
            p => p.RefundPaymentAsync("pi_test", 150m, "uah", It.IsAny<CancellationToken>(), $"ticket_refund_{ticket.Id}"),
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
        var bookingSeat1 = new BookingSeat(booking.Id, seat1.Id, 150m) { Id = Guid.NewGuid() };
        var bookingSeat2 = new BookingSeat(booking.Id, seat2.Id, 150m) { Id = Guid.NewGuid() };
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
            .Setup(p => p.RefundPaymentAsync("pi_test_2", 150m, "uah", It.IsAny<CancellationToken>(), It.IsAny<string?>()))
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
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_test_fail", 150m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket = new Ticket(bookingSeat.Id, "CODE123");
        DbContext.Tickets.Add(ticket);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_test_fail", 150m, "uah", It.IsAny<CancellationToken>(), It.IsAny<string?>()))
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

    [Fact]
    public async Task CancelTicket_HistoricalPurchasePriceRetained_WhenSessionBasePriceChanges()
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
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_hist_price", 150m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket = new Ticket(bookingSeat.Id, "CODE_HIST");
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        session.BasePrice = 300m;
        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_hist_price", 150m, "uah", It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        var request = new CancelTicketCommand
        {
            UserId = userId,
            TicketId = ticket.Id
        };

        var result = await Mediator.Send(request);

        result.Should().BeTrue();

        PaymentServiceMock.Verify(
            p => p.RefundPaymentAsync("pi_hist_price", 150m, "uah", It.IsAny<CancellationToken>(), $"ticket_refund_{ticket.Id}"),
            Times.Once);
    }

    [Fact]
    public async Task CancelTicket_ConcurrentDuplicateCancellation_UsesSameIdempotencyKeyAndDoesNotLeakConcurrencyException()
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
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_concurrent_cancellation", 150m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket = new Ticket(bookingSeat.Id, "CONCURRENT_CODE");
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var capturedKeys = new System.Collections.Concurrent.ConcurrentBag<string?>();
        var capturedAmounts = new System.Collections.Concurrent.ConcurrentBag<decimal>();
        using var barrier = new System.Threading.Barrier(2);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_concurrent_cancellation", 150m, "uah", It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Callback<string, decimal, string, CancellationToken, string?>((_, amount, _, ct, key) =>
            {
                capturedKeys.Add(key);
                capturedAmounts.Add(amount);
                barrier.SignalAndWait(ct);
            })
            .ReturnsAsync(true);

        using var scope1 = ServiceProvider.CreateScope();
        using var scope2 = ServiceProvider.CreateScope();
        var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
        var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();

        var req1 = new CancelTicketCommand { UserId = userId, TicketId = ticket.Id };
        var req2 = new CancelTicketCommand { UserId = userId, TicketId = ticket.Id };

        var task1 = mediator1.Send(req1);
        var task2 = mediator2.Send(req2);

        var results = await Task.WhenAll(task1, task2);

        results.Should().Contain(true);
        capturedKeys.Should().HaveCount(2);
        capturedKeys.Should().AllBe($"ticket_refund_{ticket.Id}");
        capturedAmounts.Should().HaveCount(2);
        capturedAmounts.Should().OnlyContain(a => a == 150m);

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
    }

    [Fact]
    public async Task CancelTicket_PostCompletionRetry_DoesNotCallRefundAgain()
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
        var bookingSeat = new BookingSeat(booking.Id, seat.Id, 150m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bookingSeat);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_retry_test", 150m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket = new Ticket(bookingSeat.Id, "RETRY_CODE");
        DbContext.Tickets.Add(ticket);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_retry_test", 150m, "uah", It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        var request = new CancelTicketCommand { UserId = userId, TicketId = ticket.Id };

        var firstResult = await Mediator.Send(request);
        firstResult.Should().BeTrue();

        PaymentServiceMock.Invocations.Clear();

        var retryResult = await Mediator.Send(request);
        retryResult.Should().BeFalse();

        PaymentServiceMock.Verify(
            p => p.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelTicket_PartialCancellation_PreservesUnrelatedTicketsAndPaymentState()
    {
        var userId = Guid.NewGuid();

        var cinema = new Cinema("StarPlex", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Main Hall", 10, 10);
        DbContext.Halls.Add(hall);

        var seat1 = new Seat(hall.Id, "1", 1, SeatType.Standard);
        var seat2 = new Seat(hall.Id, "1", 2, SeatType.Standard);
        var seat3 = new Seat(hall.Id, "1", 3, SeatType.Standard);
        DbContext.Seats.AddRange(seat1, seat2, seat3);

        var movie = new Movie(12345, "Inception", "Inception", "A thief...", 120, "poster.jpg", "backdrop.jpg", "Sci-Fi", "url", "PG-13", 8.8, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 100, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 300m, DateTime.UtcNow, BookingStatus.Confirmed)
        {
            Id = Guid.NewGuid()
        };
        var bs1 = new BookingSeat(booking.Id, seat1.Id, 100m) { Id = Guid.NewGuid() };
        var bs2 = new BookingSeat(booking.Id, seat2.Id, 100m) { Id = Guid.NewGuid() };
        var bs3 = new BookingSeat(booking.Id, seat3.Id, 100m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bs1);
        booking.BookingSeats.Add(bs2);
        booking.BookingSeats.Add(bs3);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_partial_cancel", 300m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket1 = new Ticket(bs1.Id, "TICK1");
        var ticket2 = new Ticket(bs2.Id, "TICK2");
        var ticket3 = new Ticket(bs3.Id, "TICK3");
        DbContext.Tickets.AddRange(ticket1, ticket2, ticket3);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_partial_cancel", 100m, "uah", It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        var request = new CancelTicketCommand { UserId = userId, TicketId = ticket1.Id };
        var result = await Mediator.Send(request);

        result.Should().BeTrue();

        var dbTickets = await DbContext.Tickets.AsNoTracking().ToListAsync();
        dbTickets.Should().HaveCount(2);
        dbTickets.Select(t => t.Id).Should().Contain(new[] { ticket2.Id, ticket3.Id });

        var dbBookingSeats = await DbContext.BookingSeats.AsNoTracking().ToListAsync();
        dbBookingSeats.Should().HaveCount(2);
        dbBookingSeats.Select(bs => bs.Id).Should().Contain(new[] { bs2.Id, bs3.Id });

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking!.Status.Should().Be(BookingStatus.Confirmed);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment!.Status.Should().Be(PaymentStatus.Succeeded);

        PaymentServiceMock.Verify(
            p => p.RefundPaymentAsync("pi_partial_cancel", 100m, "uah", It.IsAny<CancellationToken>(), $"ticket_refund_{ticket1.Id}"),
            Times.Once);
    }

    [Fact]
    public async Task CancelTicket_ConcurrentCancellationOfDifferentTickets_TransitionsBookingToCancelledAndPaymentToRefunded()
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

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 200, SessionStatus.Active);
        DbContext.Sessions.Add(session);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 200m, DateTime.UtcNow, BookingStatus.Confirmed)
        {
            Id = Guid.NewGuid()
        };
        var bs1 = new BookingSeat(booking.Id, seat1.Id, 100m) { Id = Guid.NewGuid() };
        var bs2 = new BookingSeat(booking.Id, seat2.Id, 100m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bs1);
        booking.BookingSeats.Add(bs2);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_concurrent_diff_tickets", 200m, PaymentStatus.Succeeded);
        DbContext.Payments.Add(payment);

        var ticket1 = new Ticket(bs1.Id, "TICK_DIFF_1");
        var ticket2 = new Ticket(bs2.Id, "TICK_DIFF_2");
        DbContext.Tickets.AddRange(ticket1, ticket2);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.RefundPaymentAsync("pi_concurrent_diff_tickets", 100m, "uah", It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        using var scope1 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
        using var scope2 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
        var mediator1 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IMediator>(scope1.ServiceProvider);
        var mediator2 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IMediator>(scope2.ServiceProvider);

        var req1 = new CancelTicketCommand { UserId = userId, TicketId = ticket1.Id };
        var req2 = new CancelTicketCommand { UserId = userId, TicketId = ticket2.Id };

        var task1 = mediator1.Send(req1);
        var task2 = mediator2.Send(req2);

        var results = await Task.WhenAll(task1, task2);

        results.Should().OnlyContain(r => r == true);

        var dbTickets = await DbContext.Tickets.AsNoTracking().Where(t => t.BookingSeat.BookingId == booking.Id).ToListAsync();
        dbTickets.Should().BeEmpty();

        var dbBookingSeats = await DbContext.BookingSeats.AsNoTracking().Where(bs => bs.BookingId == booking.Id).ToListAsync();
        dbBookingSeats.Should().BeEmpty();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.Status.Should().Be(BookingStatus.Cancelled);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.Status.Should().Be(PaymentStatus.Refunded);

        PaymentServiceMock.Verify(
            p => p.RefundPaymentAsync("pi_concurrent_diff_tickets", 100m, "uah", It.IsAny<CancellationToken>(), $"ticket_refund_{ticket1.Id}"),
            Times.Once);

        PaymentServiceMock.Verify(
            p => p.RefundPaymentAsync("pi_concurrent_diff_tickets", 100m, "uah", It.IsAny<CancellationToken>(), $"ticket_refund_{ticket2.Id}"),
            Times.Once);
    }
}
