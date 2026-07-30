using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Bookings.Commands.LockSeats;

public class LockSeatsCommandHandler : IRequestHandler<LockSeatsCommand, LockSeatsResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISeatLockService _seatLockService;
    private readonly ISeatHubService _seatHubService;

    public LockSeatsCommandHandler(IApplicationDbContext context, ISeatLockService seatLockService, ISeatHubService seatHubService)
    {
        _context = context;
        _seatLockService = seatLockService;
        _seatHubService = seatHubService;
    }

    public async Task<LockSeatsResponseDto> Handle(LockSeatsCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session == null)
            throw new NotFoundException("Session", request.SessionId);

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException("Cannot lock seats for an inactive or completed session.");

        var seats = await _context.Seats
            .AsNoTracking()
            .Where(s => s.HallId == session.HallId && request.SeatIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        if (seats.Count != request.SeatIds.Count)
            throw new InvalidOperationException("Some of the selected seats do not belong to this cinema hall.");

        if (seats.Any(s => s.Status == SeatStatus.Inactive))
            throw new InvalidOperationException("One or more selected seats are currently inactive due to maintenance.");

        var success = await _seatLockService.LockSeatsAsync(
            request.SessionId,
            request.SeatIds,
            request.UserId,
            cancellationToken);

        if (!success)
        {
            throw new InvalidOperationException("One or more selected seats are already locked or booked by another user.");
        }

        await _seatHubService.NotifySeatsLockedAsync(
            request.SessionId,
            request.SeatIds,
            request.UserId,
            cancellationToken);

        return new LockSeatsResponseDto
        {
            IsSuccess = true,
            Message = "Seats successfully locked for 10 minutes.",
            ExpiryTimeUtc = DateTime.UtcNow.AddMinutes(10)
        };
    }
}