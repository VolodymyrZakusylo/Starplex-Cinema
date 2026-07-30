using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Features.Audit.Queries.GetAuditLogs;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("logs")]
    public async Task<ActionResult<PagedAuditLogsResponseDto>> GetAuditLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? searchUser = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? actionType = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAuditLogsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchUser = searchUser,
            EntityName = entityName,
            ActionType = actionType
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}