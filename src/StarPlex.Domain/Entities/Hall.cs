using StarPlex.Domain.Common;

namespace StarPlex.Domain.Entities;

public class Hall : BaseEntity
{
    public Guid CinemaId { get; set; }
    public string Name { get; set; } = string.Empty;

    public int TotalRows { get; set; }
    public int SeatsPerRow { get; set; }

    public bool IsActive { get; set; } = true;

    public int TotalCapacity => TotalRows * SeatsPerRow;

    public Cinema Cinema { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public Hall()
    {
    }

    public Hall(Guid cinemaId, string name, int totalRows, int seatsPerRow)
    {
        CinemaId = cinemaId;
        Name = name;
        TotalRows = totalRows;
        SeatsPerRow = seatsPerRow;
        IsActive = true;
    }
}