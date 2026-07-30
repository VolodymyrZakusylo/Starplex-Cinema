namespace StarPlex.Application.Features.Bookings.Queries.GetCashierRecentSales;

public class CashierSaleDto
{
    public Guid BookingId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime SessionStartTime { get; set; }
    public decimal TotalPrice { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime BookingTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Seats { get; set; } = new();
}