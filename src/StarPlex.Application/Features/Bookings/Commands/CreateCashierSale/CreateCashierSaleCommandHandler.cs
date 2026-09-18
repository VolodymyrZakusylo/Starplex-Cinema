using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Domain.Services;
using StarPlex.Domain.Factories;

namespace StarPlex.Application.Features.Bookings.Commands.CreateCashierSale;

public class CreateCashierSaleCommandHandler : IRequestHandler<CreateCashierSaleCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateCashierSaleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateCashierSaleCommand request, CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext)
            throw new InvalidOperationException("Database context is not compatible with transactions.");

        var utcNow = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (dbContext.Database.ProviderName?.Contains("Npgsql") == true)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Sessions\" WHERE \"Id\" = {request.SessionId} FOR UPDATE", cancellationToken);
        }

        var session = await _context.Sessions
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session == null)
            throw new NotFoundException("Session", request.SessionId);

        if (!_currentUserService.IsSuperAdmin &&
            (!_currentUserService.CinemaId.HasValue || _currentUserService.CinemaId != session.Hall.CinemaId))
        {
            throw new ForbiddenException("You do not have permission to manage this cinema.");
        }

        var seats = await _context.Seats
            .Where(s => s.HallId == session.HallId && request.SeatIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        if (seats.Count != request.SeatIds.Count)
            throw new BusinessRuleException("Some of the selected seats were not found in this hall.");

        if (seats.Any(s => s.Status == SeatStatus.Inactive))
            throw new BusinessRuleException("Cannot sell tickets for an inactive or broken seat.");

        var callerUserId = _currentUserService.UserId;

        var validCallerLocks = await _context.SelectedSeats
            .Where(ss => ss.SessionId == request.SessionId &&
                         request.SeatIds.Contains(ss.SeatId) &&
                         callerUserId.HasValue &&
                         ss.UserId == callerUserId.Value &&
                         ss.LockedUntil > utcNow)
            .ToListAsync(cancellationToken);

        if (validCallerLocks.Count != request.SeatIds.Count)
        {
            throw new BusinessRuleException("Your reservation session for some of these seats has expired or is invalid.");
        }

        var alreadyTakenSeatIds = await _context.BookingSeats
            .Where(bs => bs.Booking.SessionId == request.SessionId &&
                         request.SeatIds.Contains(bs.SeatId) &&
                         (bs.Booking.Status == BookingStatus.Confirmed ||
                          bs.Booking.Status == BookingStatus.Pending))
            .Select(bs => bs.SeatId)
            .ToListAsync(cancellationToken);

        if (alreadyTakenSeatIds.Any())
        {
            var takenSeats = seats.Where(s => alreadyTakenSeatIds.Contains(s.Id)).ToList();
            var seatDescs = string.Join(", ", takenSeats.Select(s => $"Row {s.Row}, Seat {s.Number}"));
            throw new BusinessRuleException($"The following seat(s) are already booked or sold for this session: {seatDescs}.");
        }

        decimal totalPrice = 0;
        var bookingId = Guid.NewGuid();
        var bookingSeats = new List<BookingSeat>();

        foreach (var seat in seats)
        {
            decimal seatPrice = PricingCalculator.CalculateTicketPrice(session.BasePrice, seat.Type);
            totalPrice += seatPrice;

            var bookingSeat = new BookingSeat(bookingId, seat.Id, seatPrice) { Id = Guid.NewGuid() };
            bookingSeats.Add(bookingSeat);
        }

        var booking = new Booking(
            userId: Guid.Empty,
            request.SessionId,
            totalPrice,
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
            BookingStatus.Confirmed
        )
        {
            Id = bookingId,
            BookingSeats = bookingSeats
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(cancellationToken);

        if (validCallerLocks.Any())
        {
            _context.SelectedSeats.RemoveRange(validCallerLocks);
        }

        string cashierReferenceId = $"POS-{request.PaymentMethod.ToUpper()}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var payment = new Payment(booking.Id, cashierReferenceId, booking.TotalPrice, PaymentStatus.Succeeded)
        {
            Id = Guid.NewGuid(),
            PaidAt = utcNow
        };
        _context.Payments.Add(payment);

        foreach (var bookingSeat in booking.BookingSeats)
        {
            var ticket = TicketFactory.CreateForSeat(bookingSeat.Id);

            _context.Tickets.Add(ticket);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return booking.Id;
    }
}
