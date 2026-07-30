using System.Text.Json.Serialization;
using MediatR;

namespace StarPlex.Application.Features.Bookings.Commands.UnlockSeats;

public class UnlockSeatsCommand : IRequest<UnlockSeatsResponseDto>
{
    public Guid SessionId { get; set; }
    public List<Guid> SeatIds { get; set; } = new();

    [JsonIgnore]
    public Guid UserId { get; set; }
}

public class UnlockSeatsResponseDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}