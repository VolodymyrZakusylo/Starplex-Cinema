using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Bookings.DTOs;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ISeatLockService _seatLockService;

    public CreateBookingCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ISeatLockService seatLockService)
    {
        _context = context;
        _paymentService = paymentService;
        _seatLockService = seatLockService;
    }

    public async Task<BookingResponseDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var session = await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session == null)
            throw new NotFoundException("Session", request.SessionId);

        var lockedSeatIds = await _seatLockService.GetLockedSeatIdsAsync(request.SessionId, cancellationToken);

        var validUserLocksCount = await _context.SelectedSeats
            .AsNoTracking()
            .Where(ss => ss.SessionId == request.SessionId &&
                         ss.UserId == request.UserId &&
                         request.SeatIds.Contains(ss.SeatId) &&
                         lockedSeatIds.Contains(ss.SeatId))
            .CountAsync(cancellationToken);

        if (validUserLocksCount != request.SeatIds.Count)
        {
            throw new BusinessRuleException("Your reservation session for some of these seats has expired or is invalid.");
        }

        var seats = await _context.Seats
            .AsNoTracking()
            .Where(s => request.SeatIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        if (seats.Any(s => s.Status == SeatStatus.Inactive))
            throw new BusinessRuleException("One or more selected seats are undergoing technical maintenance and cannot be purchased.");

        decimal totalPrice = 0;
        var bookingId = Guid.NewGuid();
        var bookingSeats = new List<BookingSeat>();

        foreach (var seat in seats)
        {
            decimal multiplier = seat.Type switch
            {
                SeatType.VIP => 1.5m,
                SeatType.Disabled => 0.8m,
                _ => 1.0m
            };

            totalPrice += session.BasePrice * multiplier;

            var bookingSeat = new BookingSeat(bookingId, seat.Id) { Id = Guid.NewGuid() };
            bookingSeats.Add(bookingSeat);
        }

        Discount? appliedDiscount = null;
        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.Code.ToLower() == request.PromoCode.Trim().ToLower(), cancellationToken);

            if (discount != null && discount.IsActive && utcNow >= discount.ValidFrom && utcNow <= discount.ValidTo && discount.UsageCount < discount.UsageLimit)
            {
                appliedDiscount = discount;
                decimal discountFactor = discount.Percentage / 100;
                decimal discountAmount = Math.Round(totalPrice * discountFactor, 2);
                totalPrice -= discountAmount;
            }
            else
            {
                throw new BusinessRuleException("The promo code provided is invalid, expired, or has reached its usage limit.");
            }
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

        if (_context is DbContext dbContext)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.Bookings.Add(booking);

                if (appliedDiscount != null)
                {
                    appliedDiscount.UsageCount++;
                }

                await _context.SaveChangesAsync(cancellationToken);

                string clientSecret = await _paymentService.CreatePaymentIntentAsync(
                    booking.Id,
                    booking.TotalPrice,
                    "uah",
                    cancellationToken);

                string stripePaymentIntentId = clientSecret.Split("_secret_")[0];

                var payment = new Payment(booking.Id, stripePaymentIntentId, booking.TotalPrice, PaymentStatus.Pending)
                {
                    Id = Guid.NewGuid()
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return new BookingResponseDto
                {
                    BookingId = booking.Id,
                    TotalAmount = booking.TotalPrice,
                    Status = booking.Status.ToString(),
                    ClientSecret = clientSecret,
                    Message = "Booking initialized with discount and Stripe payment intent created successfully."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to initialize booking and payment: {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException("Database context is not compatible with transactions.");
    }
}