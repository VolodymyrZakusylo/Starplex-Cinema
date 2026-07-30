using StarPlex.Domain.Common;

namespace StarPlex.Domain.Entities;

public class SelectedSeat : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid SeatId { get; set; }
    public Guid UserId { get; set; }
    public DateTime LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; }

    public Session Session { get; set; } = null!;
    public Seat Seat { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow > LockedUntil;

    public SelectedSeat()
    {
    }

    public SelectedSeat(Guid sessionId, Guid seatId, Guid userId, DateTime lockedUntil)
    {
        SessionId = sessionId;
        SeatId = seatId;
        UserId = userId;
        LockedUntil = lockedUntil;
        CreatedAt = DateTime.UtcNow;
    }
}