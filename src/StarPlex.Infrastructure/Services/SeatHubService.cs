using Microsoft.AspNetCore.SignalR;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Infrastructure.Hubs;

namespace StarPlex.Infrastructure.Services;

public class SeatHubService : ISeatHubService
{
    private readonly IHubContext<SeatHub> _hubContext;

    public SeatHubService(IHubContext<SeatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifySeatsLockedAsync(Guid sessionId, List<Guid> seatIds, Guid userId, CancellationToken ct)
    {
        var groupName = $"session_{sessionId}";
        await _hubContext.Clients.Group(groupName).SendAsync("SeatsLocked", new
        {
            SessionId = sessionId,
            SeatIds = seatIds,
            LockedByUserId = userId
        }, ct);
    }

    public async Task NotifySeatsReleasedAsync(Guid sessionId, List<Guid> seatIds, CancellationToken ct)
    {
        var groupName = $"session_{sessionId}";
        await _hubContext.Clients.Group(groupName).SendAsync("SeatsReleased", new
        {
            SessionId = sessionId,
            SeatIds = seatIds
        }, ct);
    }
}