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

    public CreateCashierSaleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCashierSaleCommand request, CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext)
            throw new InvalidOperationException("Database context is not compatible with transactions.");

        var utcNow = DateTime.UtcNow;

        var session = await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session == null)
            throw new NotFoundException("Session", request.SessionId);

        var seats = await _context.Seats
            .AsNoTracking()
            .Where(s => s.HallId == session.HallId && request.SeatIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        if (seats.Count != request.SeatIds.Count)
            throw new BusinessRuleException("Some of the selected seats were not found in this hall.");

        if (seats.Any(s => s.Status == SeatStatus.Inactive))
            throw new BusinessRuleException("Cannot sell tickets for an inactive or broken seat.");

        decimal totalPrice = 0;
        var bookingId = Guid.NewGuid();
        var bookingSeats = new List<BookingSeat>();

        foreach (var seat in seats)
        {
            totalPrice += PricingCalculator.CalculateTicketPrice(session.BasePrice, seat.Type);

            var bookingSeat = new BookingSeat(bookingId, seat.Id) { Id = Guid.NewGuid() };
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(cancellationToken);

            var temporaryLocks = await _context.SelectedSeats
                .Where(ss => ss.SessionId == request.SessionId && request.SeatIds.Contains(ss.SeatId))
                .ToListAsync(cancellationToken);

            if (temporaryLocks.Any())
            {
                _context.SelectedSeats.RemoveRange(temporaryLocks);
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