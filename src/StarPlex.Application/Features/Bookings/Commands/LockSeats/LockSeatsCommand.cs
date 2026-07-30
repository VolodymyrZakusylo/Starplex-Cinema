using System.Text.Json.Serialization;
using MediatR;

namespace StarPlex.Application.Features.Bookings.Commands.LockSeats;

public class LockSeatsCommand : IRequest<LockSeatsResponseDto>
{
    public Guid SessionId { get; set; }
    public List<Guid> SeatIds { get; set; } = new();

    [JsonIgnore]
    public Guid UserId { get; set; }
}

public class LockSeatsResponseDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ExpiryTimeUtc { get; set; }
}