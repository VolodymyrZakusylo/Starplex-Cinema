using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Features.Analytics.Queries.GetAdminDashboardStats;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,CinemaManager")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("admin-stats")]
    public async Task<ActionResult<AdminDashboardStatsDto>> GetAdminStats(
    [FromQuery] int days = 14,
    [FromQuery] Guid? cinemaId = null,
    [FromQuery] string isOnline = "all",
    CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAdminDashboardStatsQuery
        {
            DaysPeriod = days,
            CinemaId = cinemaId,
            IsOnline = isOnline
        }, cancellationToken);

        return Ok(result);
    }
}