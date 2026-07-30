using MediatR;

namespace StarPlex.Application.Features.Bookings.Commands.CreateCashierSale;

public class CreateCashierSaleCommand : IRequest<Guid>
{
    public Guid SessionId { get; set; }
    public List<Guid> SeatIds { get; set; } = new();
    public string PaymentMethod { get; set; } = "Cash";
}