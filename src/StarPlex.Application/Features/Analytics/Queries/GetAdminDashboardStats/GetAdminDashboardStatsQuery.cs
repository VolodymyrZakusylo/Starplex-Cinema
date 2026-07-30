using MediatR;
using System;

namespace StarPlex.Application.Features.Analytics.Queries.GetAdminDashboardStats;

public class GetAdminDashboardStatsQuery : IRequest<AdminDashboardStatsDto>
{
    public int DaysPeriod { get; set; } = 14;
    public Guid? CinemaId { get; set; }
    public string? IsOnline { get; set; }
}