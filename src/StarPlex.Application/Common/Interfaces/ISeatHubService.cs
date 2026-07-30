namespace StarPlex.Application.Common.Interfaces;

public interface ISeatHubService
{
    Task NotifySeatsLockedAsync(Guid sessionId, List<Guid> seatIds, Guid userId, CancellationToken ct);
    Task NotifySeatsReleasedAsync(Guid sessionId, List<Guid> seatIds, CancellationToken ct);
}