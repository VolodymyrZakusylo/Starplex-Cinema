using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StarPlex.Application.Features.Bookings.Queries.GetCashierRecentSales;

public class GetCashierRecentSalesQueryHandler : IRequestHandler<GetCashierRecentSalesQuery, List<CashierSaleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCashierRecentSalesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CashierSaleDto>> Handle(GetCashierRecentSalesQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var sales = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Session).ThenInclude(s => s.Movie)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Where(b => b.UserId == Guid.Empty
                     && b.BookingTime >= today)
            .OrderByDescending(b => b.BookingTime)
            .ToListAsync(cancellationToken);

        var bookingIds = sales.Select(s => s.Id).ToList();

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => bookingIds.Contains(p.BookingId))
            .ToListAsync(cancellationToken);

        return sales.Select(b =>
        {
            var payment = payments.FirstOrDefault(p => p.BookingId == b.Id);

            string method = "Готівка";
            if (payment != null && payment.StripePaymentIntentId.Contains("CARD"))
            {
                method = "Картка (Термінал)";
            }

            return new CashierSaleDto
            {
                BookingId = b.Id,
                OrderNumber = b.Id.ToString()[..8].ToUpper(),
                MovieTitle = b.Session.Movie.Title,
                SessionStartTime = b.Session.StartTime,
                TotalPrice = b.TotalPrice,
                PaymentMethod = method,
                BookingTime = b.BookingTime,
                Status = b.Status.ToString(),
                Seats = b.BookingSeats
                    .Select(bs => $"{bs.Seat.Row}р. {bs.Seat.Number}м.")
                    .ToList()
            };
        }).ToList();
    }
}