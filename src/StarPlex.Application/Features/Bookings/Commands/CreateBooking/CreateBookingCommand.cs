using MediatR;
using System.Text.Json.Serialization;
using StarPlex.Application.Features.Bookings.DTOs;

namespace StarPlex.Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommand : IRequest<BookingResponseDto>
{
    public Guid SessionId { get; set; }
    public List<Guid> SeatIds { get; set; } = new();

    public string? PromoCode { get; set; }

    [JsonIgnore]
    public Guid UserId { get; set; }
}