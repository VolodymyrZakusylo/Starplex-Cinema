using StarPlex.Domain.Common;
using StarPlex.Domain.Enums;

namespace StarPlex.Domain.Entities;

public class Seat : BaseEntity
{
    public Guid HallId { get; set; }
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public SeatType Type { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Active;
    public Hall Hall { get; set; } = null!;

    public Seat()
    {
    }

    public Seat(Guid hallId, string row, int number, SeatType type, SeatStatus status = SeatStatus.Active)
    {
        HallId = hallId;
        Row = row;
        Number = number;
        Type = type;
        Status = status;
    }
}