using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace StarPlex.Infrastructure.Hubs;

public class SeatHub : Hub
{
    private readonly ILogger<SeatHub> _logger;

    public SeatHub(ILogger<SeatHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinSessionRoom(Guid sessionId)
    {
        var groupName = $"session_{sessionId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined room {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveSessionRoom(Guid sessionId)
    {
        var groupName = $"session_{sessionId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left room {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task SeatLocked(Guid sessionId, Guid seatId, Guid userId)
    {
        var groupName = $"session_{sessionId}";
        await Clients.OthersInGroup(groupName).SendAsync("OnSeatLocked", seatId, userId);
        _logger.LogInformation("Seat {SeatId} locked in session {SessionId} by user {UserId}", seatId, sessionId, userId);
    }

    public async Task SeatUnlocked(Guid sessionId, Guid seatId)
    {
        var groupName = $"session_{sessionId}";
        await Clients.OthersInGroup(groupName).SendAsync("OnSeatUnlocked", seatId);
        _logger.LogInformation("Seat {SeatId} unlocked in session {SessionId}", seatId, sessionId);
    }
}