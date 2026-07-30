using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Analytics.Queries.GetAdminDashboardStats;

public class GetAdminDashboardStatsQueryHandler : IRequestHandler<GetAdminDashboardStatsQuery, AdminDashboardStatsDto>
{
    private readonly IApplicationDbContext _context;

    public GetAdminDashboardStatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardStatsDto> Handle(GetAdminDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-request.DaysPeriod);

        var bookingsQuery = _context.Bookings.AsNoTracking();
        var seatsQuery = _context.BookingSeats.AsNoTracking();
        var sessionsQuery = _context.Sessions.AsNoTracking();
        var paymentsQuery = _context.Payments.AsNoTracking();

        if (request.CinemaId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(b => b.Session.Hall.CinemaId == request.CinemaId.Value);
            seatsQuery = seatsQuery.Where(bs => bs.Booking.Session.Hall.CinemaId == request.CinemaId.Value);
            sessionsQuery = sessionsQuery.Where(s => s.Hall.CinemaId == request.CinemaId.Value);
            paymentsQuery = paymentsQuery.Where(p => p.Booking.Session.Hall.CinemaId == request.CinemaId.Value);
        }

        if (!string.IsNullOrEmpty(request.IsOnline) && request.IsOnline != "all")
        {
            if (request.IsOnline == "online")
            {
                bookingsQuery = bookingsQuery.Where(b => b.UserId != null && b.UserId != Guid.Empty);
                seatsQuery = seatsQuery.Where(bs => bs.Booking.UserId != null && bs.Booking.UserId != Guid.Empty);
                paymentsQuery = paymentsQuery.Where(p => p.Booking.UserId != null && p.Booking.UserId != Guid.Empty);
            }
            else if (request.IsOnline == "boxoffice")
            {
                bookingsQuery = bookingsQuery.Where(b => b.UserId == null || b.UserId == Guid.Empty);
                seatsQuery = seatsQuery.Where(bs => bs.Booking.UserId == null || bs.Booking.UserId == Guid.Empty);
                paymentsQuery = paymentsQuery.Where(p => p.Booking.UserId == null || p.Booking.UserId == Guid.Empty);
            }
        }

        var totalRevenue = await bookingsQuery
            .Where(b => b.Status == BookingStatus.Confirmed && b.BookingTime >= startDate)
            .SumAsync(b => b.TotalPrice, cancellationToken);

        var totalTickets = await seatsQuery
            .Where(bs => bs.Booking.Status == BookingStatus.Confirmed && bs.Booking.BookingTime >= startDate)
            .CountAsync(cancellationToken);

        var activeSessionsCount = await sessionsQuery
            .CountAsync(s => s.StartTime > DateTime.UtcNow, cancellationToken);

        var totalBookingsCount = await bookingsQuery
            .Where(b => b.Status == BookingStatus.Confirmed && b.BookingTime >= startDate)
            .CountAsync(cancellationToken);

        decimal averageOrderValue = totalBookingsCount > 0 ? totalRevenue / totalBookingsCount : 0;

        var totalInitialPaymentsVolume = await paymentsQuery
            .Where(p => p.PaidAt >= startDate)
            .SumAsync(p => p.Amount, cancellationToken);

        var refundedAmount = totalInitialPaymentsVolume - totalRevenue;
        if (refundedAmount < 0) refundedAmount = 0;

        var dbChartItems = await bookingsQuery
            .Where(b => b.Status == BookingStatus.Confirmed && b.BookingTime >= startDate)
            .GroupBy(b => b.BookingTime.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(b => b.TotalPrice),
                TicketsCount = g.Sum(b => b.BookingSeats.Count)
            })
            .ToListAsync(cancellationToken);

        var fullChart = new List<RevenueChartItemDto>();
        for (int i = request.DaysPeriod; i >= 0; i--)
        {
            var dateToCheck = DateTime.UtcNow.Date.AddDays(-i);
            var isoDateString = dateToCheck.ToString("yyyy-MM-dd");

            var existingDay = dbChartItems.FirstOrDefault(c => c.Date == dateToCheck);
            if (existingDay != null)
            {
                fullChart.Add(new RevenueChartItemDto
                {
                    Date = isoDateString,
                    Revenue = existingDay.Revenue,
                    TicketsCount = existingDay.TicketsCount
                });
            }
            else
            {
                fullChart.Add(new RevenueChartItemDto { Date = isoDateString, Revenue = 0, TicketsCount = 0 });
            }
        }

        var topMovies = await seatsQuery
            .Where(bs => bs.Booking.Status == BookingStatus.Confirmed && bs.Booking.BookingTime >= startDate)
            .GroupBy(bs => bs.Booking.Session.Movie.Title)
            .Select(g => new MoviePopularityDto
            {
                MovieTitle = g.Key,
                TicketsSold = g.Count(),
                Earnings = g.Sum(bs => bs.Booking.TotalPrice / bs.Booking.BookingSeats.Count)
            })
            .OrderByDescending(m => m.TicketsSold)
            .Take(5)
            .ToListAsync(cancellationToken);

        var seatTypeStats = await seatsQuery
            .Where(bs => bs.Booking.Status == BookingStatus.Confirmed && bs.Booking.BookingTime >= startDate)
            .GroupBy(bs => bs.Seat.Type)
            .Select(g => new SeatTypeBreakdownDto
            {
                Type = g.Key.ToString(),
                Count = g.Count(),
                Revenue = g.Sum(bs => bs.Booking.TotalPrice / bs.Booking.BookingSeats.Count)
            })
            .ToListAsync(cancellationToken);

        return new AdminDashboardStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalTicketsSold = totalTickets,
            ActiveSessionsCount = activeSessionsCount,
            RefundedAmount = refundedAmount,
            AverageOrderValue = Math.Round(averageOrderValue, 2),
            RevenueChart = fullChart,
            TopMovies = topMovies,
            SeatTypeStats = seatTypeStats
        };
    }
}