using MediatR;

namespace StarPlex.Application.Features.Bookings.Queries.GetCashierRecentSales;

public class GetCashierRecentSalesQuery : IRequest<List<CashierSaleDto>>
{
    public Guid SessionId { get; set; }
}