namespace StarPlex.Application.Features.Bookings.DTOs;

public class BookingResponseDto
{
    public Guid BookingId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}