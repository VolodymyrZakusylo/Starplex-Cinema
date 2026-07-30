using StarPlex.Domain.Enums;

public class AdminSeatDetailsDto
{
    public Guid Id { get; set; }
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public SeatType Type { get; set; }
    public SeatStatus Status { get; set; }
}