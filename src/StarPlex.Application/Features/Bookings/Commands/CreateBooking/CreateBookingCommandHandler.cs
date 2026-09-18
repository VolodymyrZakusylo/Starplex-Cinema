using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Bookings.DTOs;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Domain.Services;

namespace StarPlex.Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;

    public CreateBookingCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService)
    {
        _context = context;
        _paymentService = paymentService;
    }

    public async Task<BookingResponseDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext)
            throw new InvalidOperationException("Database context is not compatible with transactions.");

        var utcNow = DateTime.UtcNow;
        var requestedSeatSet = request.SeatIds.ToHashSet();

        Booking targetBooking;
        Payment targetPayment;

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            // Lock session row to serialize concurrent booking attempts for the same session
            if (dbContext.Database.ProviderName?.Contains("Npgsql") == true)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Sessions\" WHERE \"Id\" = {request.SessionId} FOR UPDATE", cancellationToken);
            }

            utcNow = DateTime.UtcNow;

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

            if (session == null)
                throw new NotFoundException("Session", request.SessionId);

            // Ensure caller holds an active lock for every requested seat
            var validCallerLocks = await _context.SelectedSeats
                .Where(ss => ss.SessionId == request.SessionId &&
                             request.SeatIds.Contains(ss.SeatId) &&
                             ss.UserId == request.UserId &&
                             ss.LockedUntil > utcNow)
                .ToListAsync(cancellationToken);

            if (validCallerLocks.Count != request.SeatIds.Count)
            {
                throw new BusinessRuleException("Your reservation session for some of these seats has expired or is invalid.");
            }

            // Reuse an active pending booking if the customer is retrying the exact same seat set
            var candidatePendingBookings = await _context.Bookings
                .Include(b => b.BookingSeats)
                .Include(b => b.Payment)
                .Where(b => b.SessionId == request.SessionId &&
                            b.UserId == request.UserId &&
                            b.Status == BookingStatus.Pending)
                .ToListAsync(cancellationToken);

            var reusableBooking = candidatePendingBookings.FirstOrDefault(b =>
                b.BookingSeats.Count == requestedSeatSet.Count &&
                b.BookingSeats.Select(bs => bs.SeatId).ToHashSet().SetEquals(requestedSeatSet));

            // Enforce seat occupancy against any active booking except the exact attempt being reused
            var occupiedSeatQuery = _context.BookingSeats
                .Where(bs => bs.Booking.SessionId == request.SessionId &&
                             request.SeatIds.Contains(bs.SeatId) &&
                             (bs.Booking.Status == BookingStatus.Confirmed || bs.Booking.Status == BookingStatus.Pending));

            if (reusableBooking != null)
            {
                occupiedSeatQuery = occupiedSeatQuery.Where(bs => bs.BookingId != reusableBooking.Id);
            }

            var conflictingSeatIds = await occupiedSeatQuery
                .Select(bs => bs.SeatId)
                .ToListAsync(cancellationToken);

            if (conflictingSeatIds.Any())
            {
                var seatsForDesc = await _context.Seats
                    .Where(s => conflictingSeatIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);
                var seatDescs = string.Join(", ", seatsForDesc.Select(s => $"Row {s.Row}, Seat {s.Number}"));
                throw new BusinessRuleException($"The following seat(s) are already booked or sold for this session: {seatDescs}.");
            }

            if (reusableBooking != null)
            {
                targetBooking = reusableBooking;
                if (reusableBooking.Payment != null)
                {
                    targetPayment = reusableBooking.Payment;
                }
                else
                {
                    targetPayment = new Payment(reusableBooking.Id, string.Empty, reusableBooking.TotalPrice, PaymentStatus.Pending)
                    {
                        Id = Guid.NewGuid()
                    };
                    _context.Payments.Add(targetPayment);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                var seats = await _context.Seats
                    .Where(s => s.HallId == session.HallId && request.SeatIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                if (seats.Count != request.SeatIds.Count)
                    throw new BusinessRuleException("Some of the selected seats were not found in this hall.");

                if (seats.Any(s => s.Status == SeatStatus.Inactive))
                    throw new BusinessRuleException("One or more selected seats are undergoing technical maintenance and cannot be purchased.");

                decimal totalBasePrice = 0;
                var basePrices = new List<decimal>();
                foreach (var seat in seats)
                {
                    decimal seatBasePrice = PricingCalculator.CalculateTicketPrice(session.BasePrice, seat.Type);
                    basePrices.Add(seatBasePrice);
                    totalBasePrice += seatBasePrice;
                }

                decimal totalPrice = totalBasePrice;
                Discount? appliedDiscount = null;
                if (!string.IsNullOrWhiteSpace(request.PromoCode))
                {
                    var discount = await _context.Discounts
                        .FirstOrDefaultAsync(d => d.Code.ToLower() == request.PromoCode.Trim().ToLower(), cancellationToken);

                    if (discount != null && discount.IsActive && utcNow >= discount.ValidFrom && utcNow <= discount.ValidTo && discount.UsageCount < discount.UsageLimit)
                    {
                        appliedDiscount = discount;
                        totalPrice = PricingCalculator.ApplyDiscount(totalPrice, discount.Percentage);
                    }
                    else
                    {
                        throw new BusinessRuleException("The promo code provided is invalid, expired, or has reached its usage limit.");
                    }
                }

                var bookingId = Guid.NewGuid();
                var bookingSeats = new List<BookingSeat>();
                decimal accumulatedPurchasePrice = 0;

                for (int i = 0; i < seats.Count; i++)
                {
                    decimal seatPurchasePrice;
                    if (i == seats.Count - 1)
                    {
                        seatPurchasePrice = totalPrice - accumulatedPurchasePrice;
                    }
                    else
                    {
                        decimal ratio = totalBasePrice > 0 ? totalPrice / totalBasePrice : 0;
                        seatPurchasePrice = Math.Round(basePrices[i] * ratio, 2, MidpointRounding.AwayFromZero);
                        accumulatedPurchasePrice += seatPurchasePrice;
                    }

                    var bookingSeat = new BookingSeat(bookingId, seats[i].Id, seatPurchasePrice) { Id = Guid.NewGuid() };
                    bookingSeats.Add(bookingSeat);
                }

                var booking = new Booking(
                    request.UserId,
                    request.SessionId,
                    totalPrice,
                    DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
                    BookingStatus.Pending
                )
                {
                    Id = bookingId,
                    BookingSeats = bookingSeats,
                    DiscountId = appliedDiscount?.Id
                };

                if (appliedDiscount != null)
                {
                    appliedDiscount.UsageCount++;
                }

                var payment = new Payment(booking.Id, string.Empty, booking.TotalPrice, PaymentStatus.Pending)
                {
                    Id = Guid.NewGuid()
                };

                _context.Bookings.Add(booking);
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                targetBooking = booking;
                targetPayment = payment;
            }
        }

        // Execute external Stripe payment intent creation outside the database transaction boundary to avoid holding DB connections or locks during external API calls.
        string clientSecret = await _paymentService.CreatePaymentIntentAsync(
            targetBooking.Id,
            targetBooking.TotalPrice,
            "uah",
            targetBooking.Id.ToString(),
            cancellationToken);

        string stripePaymentIntentId = clientSecret.Split("_secret_")[0];

        if (targetPayment.StripePaymentIntentId != stripePaymentIntentId)
        {
            targetPayment.StripePaymentIntentId = stripePaymentIntentId;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new BookingResponseDto
        {
            BookingId = targetBooking.Id,
            TotalAmount = targetBooking.TotalPrice,
            Status = targetBooking.Status.ToString(),
            ClientSecret = clientSecret,
            Message = "Booking initialized with discount and Stripe payment intent created successfully."
        };
    }
}