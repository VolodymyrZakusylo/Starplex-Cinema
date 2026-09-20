using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace StarPlex.Infrastructure.Hubs;

[Authorize]
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
}