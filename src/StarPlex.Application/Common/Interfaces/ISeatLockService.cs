namespace StarPlex.Application.Common.Interfaces;

public interface ISeatLockService
{
    Task<bool> LockSeatsAsync(Guid sessionId, List<Guid> seatIds, Guid userId, CancellationToken ct);
    Task ReleaseSeatLockAsync(Guid sessionId, Guid userId, CancellationToken ct);
    Task<List<Guid>> GetLockedSeatIdsAsync(Guid sessionId, CancellationToken ct);
}