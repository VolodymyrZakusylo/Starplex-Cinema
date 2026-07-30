using StarPlex.Domain.Common;
using StarPlex.Domain.Enums;

namespace StarPlex.Domain.Entities;

public class Session : BaseEntity
{
    public const int CleanUpDurationInMinutes = 20;
    public Guid MovieId { get; set; }
    public Guid HallId { get; set; }
    public DateTime StartTime { get; set; }
    public int MovieDurationInMinutes { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal BasePrice { get; set; }
    public SessionStatus Status { get; set; }

    public Movie Movie { get; set; } = null!;
    public Hall Hall { get; set; } = null!;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<SelectedSeat> SelectedSeats { get; set; } = new List<SelectedSeat>();

    public DateTime EndTime => StartTime.AddMinutes(MovieDurationInMinutes + CleanUpDurationInMinutes);

    public Session()
    {
    }

    public Session(Guid movieId, Guid hallId, DateTime startTime, int movieDurationInMinutes, decimal originalPrice, decimal basePrice, SessionStatus status)
    {
        MovieId = movieId;
        HallId = hallId;
        StartTime = startTime;
        MovieDurationInMinutes = movieDurationInMinutes;
        OriginalPrice = originalPrice;
        BasePrice = basePrice;
        Status = status;
    }
}