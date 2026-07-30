using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Halls.DTOs;

public class HallDto
{
    public Guid Id { get; set; }
    public Guid CinemaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SeatsPerRow { get; set; }
    public int TotalCapacity { get; set; }
    public bool IsActive { get; set; }
    public List<AdminSeatDetailsDto> Seats { get; set; } = new();
}