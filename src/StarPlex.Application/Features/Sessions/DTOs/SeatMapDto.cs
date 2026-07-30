using System.Text.Json.Serialization;

namespace StarPlex.Application.Features.Sessions.DTOs;

public class SeatMapDto
{
    public Guid SeatId { get; set; }
    public string Row { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public string SeatType { get; set; } = "Standard";
    public decimal PriceMultiplier { get; set; } = 1.0m;
    public string Status { get; set; } = "Available";
    public Guid? LockedByUserId { get; set; }

    [JsonIgnore]
    public int RowNumberForSorting { get; set; }
}