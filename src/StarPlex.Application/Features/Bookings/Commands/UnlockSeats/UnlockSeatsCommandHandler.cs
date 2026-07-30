using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Bookings.Commands.UnlockSeats;

public class UnlockSeatsCommandHandler : IRequestHandler<UnlockSeatsCommand, UnlockSeatsResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISeatHubService _seatHubService;

    public UnlockSeatsCommandHandler(IApplicationDbContext context, ISeatHubService seatHubService)
    {
        _context = context;
        _seatHubService = seatHubService;
    }

    public async Task<UnlockSeatsResponseDto> Handle(UnlockSeatsCommand request, CancellationToken cancellationToken)
    {
        var userLocks = await _context.SelectedSeats
            .Where(ss => ss.SessionId == request.SessionId
                         && request.SeatIds.Contains(ss.SeatId)
                         && ss.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        if (!userLocks.Any())
        {
            return new UnlockSeatsResponseDto
            {
                IsSuccess = false,
                Message = "No active locks found for the specified seats by this user."
            };
        }

        _context.SelectedSeats.RemoveRange(userLocks);
        await _context.SaveChangesAsync(cancellationToken);

        await _seatHubService.NotifySeatsReleasedAsync(request.SessionId, request.SeatIds, cancellationToken);

        return new UnlockSeatsResponseDto
        {
            IsSuccess = true,
            Message = "Seats successfully unlocked."
        };
    }
}