using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarPlex.Application.Features.Discounts.Commands.ApplyPromoCode;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Discounts;

public class ApplyPromoCodeCommandHandlerTests : IntegrationTestBase
{
    public ApplyPromoCodeCommandHandlerTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ApplyPromoCode_WhenSuccessful_ShouldUpdateStripeAndPersistDiscountedTotals()
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

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 100, SessionStatus.Active);
        DbContext.Sessions.Add(session);

        var discount = new Discount
        {
            Code = "SUMMER20",
            Name = "Summer Promo",
            Percentage = 20,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UsageLimit = 100,
            UsageCount = 0
        };
        DbContext.Discounts.Add(discount);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 200m, DateTime.UtcNow, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bs1 = new BookingSeat(booking.Id, seat1.Id, 100m) { Id = Guid.NewGuid() };
        var bs2 = new BookingSeat(booking.Id, seat2.Id, 100m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bs1);
        booking.BookingSeats.Add(bs2);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_promo_succ", 200m, PaymentStatus.Pending);
        DbContext.Payments.Add(payment);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.UpdatePaymentIntentAmountAsync("pi_promo_succ", 160m, "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await Mediator.Send(new ApplyPromoCodeCommand
        {
            BookingId = booking.Id,
            PromoCode = "SUMMER20",
            UserId = userId
        });

        result.IsSuccess.Should().BeTrue();
        result.NewTotalPrice.Should().Be(160m);

        PaymentServiceMock.Verify(
            p => p.UpdatePaymentIntentAmountAsync("pi_promo_succ", 160m, "uah", It.IsAny<CancellationToken>()),
            Times.Once);

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking!.TotalPrice.Should().Be(160m);
        dbBooking.DiscountId.Should().Be(discount.Id);

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment!.Amount.Should().Be(160m);

        var dbDiscount = await DbContext.Discounts.AsNoTracking().FirstOrDefaultAsync(d => d.Id == discount.Id);
        dbDiscount!.UsageCount.Should().Be(1);

        var dbSeats = await DbContext.BookingSeats.AsNoTracking().Where(bs => bs.BookingId == booking.Id).ToListAsync();
        dbSeats.Should().HaveCount(2);
        dbSeats.Sum(bs => bs.PurchasePrice).Should().Be(160m);
        dbSeats[0].PurchasePrice.Should().Be(80m);
        dbSeats[1].PurchasePrice.Should().Be(80m);
    }

    [Fact]
    public async Task ApplyPromoCode_WhenStripeUpdateFails_ShouldLeaveDatabaseStateUnchanged()
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

        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 100, SessionStatus.Active);
        DbContext.Sessions.Add(session);

        var discount = new Discount
        {
            Code = "SUMMER20",
            Name = "Summer Promo",
            Percentage = 20,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UsageLimit = 100,
            UsageCount = 0
        };
        DbContext.Discounts.Add(discount);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 200m, DateTime.UtcNow, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bs1 = new BookingSeat(booking.Id, seat1.Id, 100m) { Id = Guid.NewGuid() };
        var bs2 = new BookingSeat(booking.Id, seat2.Id, 100m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bs1);
        booking.BookingSeats.Add(bs2);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_promo_fail", 200m, PaymentStatus.Pending);
        DbContext.Payments.Add(payment);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.UpdatePaymentIntentAmountAsync("pi_promo_fail", 160m, "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await Mediator.Send(new ApplyPromoCodeCommand
        {
            BookingId = booking.Id,
            PromoCode = "SUMMER20",
            UserId = userId
        });

        result.IsSuccess.Should().BeFalse();

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking!.TotalPrice.Should().Be(200m);
        dbBooking.DiscountId.Should().BeNull();

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment!.Amount.Should().Be(200m);

        var dbDiscount = await DbContext.Discounts.AsNoTracking().FirstOrDefaultAsync(d => d.Id == discount.Id);
        dbDiscount!.UsageCount.Should().Be(0);

        var dbSeats = await DbContext.BookingSeats.AsNoTracking().Where(bs => bs.BookingId == booking.Id).ToListAsync();
        dbSeats.Should().HaveCount(2);
        dbSeats.Sum(bs => bs.PurchasePrice).Should().Be(200m);
        dbSeats[0].PurchasePrice.Should().Be(100m);
        dbSeats[1].PurchasePrice.Should().Be(100m);
    }

    [Fact]
    public async Task ApplyPromoCode_WithNonEvenDiscountDivision_PreservesSumInvariants()
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

        var discount = new Discount
        {
            Code = "PROMO15",
            Name = "15 Percent Off",
            Percentage = 15,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UsageLimit = 100,
            UsageCount = 0
        };
        DbContext.Discounts.Add(discount);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 300m, DateTime.UtcNow, BookingStatus.Pending)
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

        var payment = new Payment(booking.Id, "pi_promo_uneven", 300m, PaymentStatus.Pending);
        DbContext.Payments.Add(payment);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.UpdatePaymentIntentAmountAsync("pi_promo_uneven", 255m, "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await Mediator.Send(new ApplyPromoCodeCommand
        {
            BookingId = booking.Id,
            PromoCode = "PROMO15",
            UserId = userId
        });

        result.IsSuccess.Should().BeTrue();
        result.NewTotalPrice.Should().Be(255m);

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        var dbSeats = await DbContext.BookingSeats.AsNoTracking().Where(bs => bs.BookingId == booking.Id).ToListAsync();

        dbSeats.Sum(bs => bs.PurchasePrice).Should().Be(dbBooking!.TotalPrice);
        dbBooking!.TotalPrice.Should().Be(dbPayment!.Amount);
        dbPayment!.Amount.Should().Be(255m);
    }

    [Fact]
    public async Task ApplyPromoCode_CustomerBAppliesPromoToCustomerABooking_RejectsWithUnauthorizedError()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var cinema = new Cinema("StarPlex", "Main St 1", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        var hall = new Hall(cinema.Id, "Main Hall", 10, 10);
        DbContext.Halls.Add(hall);
        var seat = new Seat(hall.Id, "1", 1, SeatType.Standard);
        DbContext.Seats.Add(seat);
        var movie = new Movie(12345, "Inception", "Inception", "A thief...", 120, "poster.jpg", "backdrop.jpg", "Sci-Fi", "url", "PG-13", 8.8, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);
        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 100, SessionStatus.Active);
        DbContext.Sessions.Add(session);

        var discount = new Discount
        {
            Code = "PROMO10",
            Name = "10 Percent Off",
            Percentage = 10,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UsageLimit = 100,
            UsageCount = 0
        };
        DbContext.Discounts.Add(discount);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userA, session.Id, 100m, DateTime.UtcNow, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bs = new BookingSeat(booking.Id, seat.Id, 100m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bs);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_owner_test", 100m, PaymentStatus.Pending);
        DbContext.Payments.Add(payment);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        var result = await Mediator.Send(new ApplyPromoCodeCommand
        {
            BookingId = booking.Id,
            PromoCode = "PROMO10",
            UserId = userB
        });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("permission");

        PaymentServiceMock.Verify(
            p => p.UpdatePaymentIntentAmountAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking!.TotalPrice.Should().Be(100m);
        dbBooking.DiscountId.Should().BeNull();

        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        dbPayment!.Amount.Should().Be(100m);

        var dbDiscount = await DbContext.Discounts.AsNoTracking().FirstOrDefaultAsync(d => d.Id == discount.Id);
        dbDiscount!.UsageCount.Should().Be(0);
    }

    [Fact]
    public async Task ApplyPromoCode_ConcurrentPromoApplications_OnlyOneSucceedsAndStripeMatchesDB()
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
        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 200, SessionStatus.Active);
        DbContext.Sessions.Add(session);

        var discount10 = new Discount
        {
            Code = "PROMO10",
            Name = "10 Percent",
            Percentage = 10,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UsageLimit = 100,
            UsageCount = 0
        };
        var discount20 = new Discount
        {
            Code = "PROMO20",
            Name = "20 Percent",
            Percentage = 20,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UsageLimit = 100,
            UsageCount = 0
        };
        DbContext.Discounts.AddRange(discount10, discount20);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 200m, DateTime.UtcNow, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bs = new BookingSeat(booking.Id, seat.Id, 200m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bs);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_concurrent_promo", 200m, PaymentStatus.Pending);
        DbContext.Payments.Add(payment);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.UpdatePaymentIntentAmountAsync("pi_concurrent_promo", It.IsAny<decimal>(), "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        using var scope1 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
        using var scope2 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
        var mediator1 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MediatR.IMediator>(scope1.ServiceProvider);
        var mediator2 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MediatR.IMediator>(scope2.ServiceProvider);

        var cmd1 = new ApplyPromoCodeCommand { BookingId = booking.Id, PromoCode = "PROMO10", UserId = userId };
        var cmd2 = new ApplyPromoCodeCommand { BookingId = booking.Id, PromoCode = "PROMO20", UserId = userId };

        var task1 = mediator1.Send(cmd1);
        var task2 = mediator2.Send(cmd2);

        var results = await Task.WhenAll(task1, task2);

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => !r.IsSuccess).Should().Be(1);

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        var dbSeats = await DbContext.BookingSeats.AsNoTracking().Where(b => b.BookingId == booking.Id).ToListAsync();

        dbBooking.Should().NotBeNull();
        dbBooking!.DiscountId.Should().NotBeNull();
        dbBooking.TotalPrice.Should().Be(dbPayment!.Amount);
        dbSeats.Sum(s => s.PurchasePrice).Should().Be(dbBooking.TotalPrice);

        PaymentServiceMock.Verify(
            p => p.UpdatePaymentIntentAmountAsync("pi_concurrent_promo", dbBooking.TotalPrice, "uah", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyPromoCode_ConcurrentWithConfirmation_MaintainsConsistentState()
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
        var session = new Session(movie.Id, hall.Id, DateTime.UtcNow.AddDays(1), 120, 100, 200, SessionStatus.Active);
        DbContext.Sessions.Add(session);

        var discount = new Discount
        {
            Code = "SUMMER10",
            Name = "10 Percent",
            Percentage = 10,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UsageLimit = 100,
            UsageCount = 0
        };
        DbContext.Discounts.Add(discount);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var booking = new Booking(userId, session.Id, 200m, DateTime.UtcNow, BookingStatus.Pending)
        {
            Id = Guid.NewGuid()
        };
        var bs = new BookingSeat(booking.Id, seat.Id, 200m) { Id = Guid.NewGuid() };
        booking.BookingSeats.Add(bs);
        DbContext.Bookings.Add(booking);

        var payment = new Payment(booking.Id, "pi_concurrent_conf", 200m, PaymentStatus.Pending);
        DbContext.Payments.Add(payment);

        DbContext.SelectedSeats.Add(new SelectedSeat(session.Id, seat.Id, userId, DateTime.UtcNow.AddMinutes(10)));
        await DbContext.SaveChangesAsync(CancellationToken.None);

        PaymentServiceMock
            .Setup(p => p.UpdatePaymentIntentAmountAsync("pi_concurrent_conf", 180m, "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        PaymentServiceMock
            .Setup(p => p.VerifyPaymentIntentAsync("pi_concurrent_conf", booking.Id, It.IsAny<decimal>(), "uah", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        CurrentUserServiceMock.SetupGet(u => u.UserId).Returns(userId);

        using var barrier = new System.Threading.Barrier(2);

        using var scope1 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
        using var scope2 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(ServiceProvider);
        var mediator1 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IMediator>(scope1.ServiceProvider);
        var mediator2 = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IMediator>(scope2.ServiceProvider);

        var promoTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await mediator1.Send(new ApplyPromoCodeCommand { BookingId = booking.Id, PromoCode = "SUMMER10", UserId = userId });
        });

        var confirmTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await mediator2.Send(new StarPlex.Application.Features.Bookings.Commands.ConfirmBooking.ConfirmBookingCommand { BookingId = booking.Id });
        });

        await Task.WhenAll(promoTask, confirmTask);

        var dbBooking = await DbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == booking.Id);
        var dbPayment = await DbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        var dbSeats = await DbContext.BookingSeats.AsNoTracking().Where(b => b.BookingId == booking.Id).ToListAsync();
        var dbTickets = await DbContext.Tickets.AsNoTracking().Where(t => t.BookingSeat.BookingId == booking.Id).ToListAsync();

        dbBooking.Should().NotBeNull();
        dbPayment.Should().NotBeNull();

        dbBooking!.TotalPrice.Should().Be(dbPayment!.Amount);
        dbSeats.Sum(s => s.PurchasePrice).Should().Be(dbBooking.TotalPrice);

        if (dbBooking.Status == BookingStatus.Confirmed)
        {
            dbPayment.Status.Should().Be(PaymentStatus.Succeeded);
            dbTickets.Should().HaveCount(1);
        }
        else
        {
            dbBooking.Status.Should().Be(BookingStatus.Pending);
            dbPayment.Status.Should().Be(PaymentStatus.Pending);
            dbTickets.Should().BeEmpty();
        }
    }
}
