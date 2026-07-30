using System.Collections.Generic;

namespace StarPlex.Application.Features.Analytics.Queries.GetAdminDashboardStats;

public class AdminDashboardStatsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalTicketsSold { get; set; }
    public int ActiveSessionsCount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public double AverageHallOccupancyPercent { get; set; }

    public List<RevenueChartItemDto> RevenueChart { get; set; } = new();
    public List<MoviePopularityDto> TopMovies { get; set; } = new();
    public List<SeatTypeBreakdownDto> SeatTypeStats { get; set; } = new();
}

public class RevenueChartItemDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int TicketsCount { get; set; }
}

public class MoviePopularityDto
{
    public string MovieTitle { get; set; } = string.Empty;
    public int TicketsSold { get; set; }
    public decimal Earnings { get; set; }
}

public class SeatTypeBreakdownDto
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}