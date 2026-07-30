using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Features.Sessions.Commands.CancelSession;
using StarPlex.Application.Features.Sessions.Commands.CreateSession;
using StarPlex.Application.Features.Sessions.Commands.GenerateSchedule;
using StarPlex.Application.Features.Sessions.Commands.UpdateSession;
using StarPlex.Application.Features.Sessions.DTOs;
using StarPlex.Application.Features.Sessions.Queries.GetSessions;
using StarPlex.Application.Features.Sessions.Queries.GetSessionsByCinemaId;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<SessionDto>>> GetSessions(
        [FromQuery] Guid? movieId,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSessionsQuery { MovieId = movieId, Date = date }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("cinema/{cinemaId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SessionDto>>> GetSessionsByCinemaId(Guid cinemaId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSessionsByCinemaIdQuery { CinemaId = cinemaId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "CinemaManager,SuperAdmin")]
    public async Task<ActionResult<Guid>> CreateSession([FromBody] CreateSessionCommand command, CancellationToken cancellationToken)
    {
        var sessionId = await _mediator.Send(command, cancellationToken);
        return CreatedAtRoute(new { id = sessionId }, sessionId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "CinemaManager,SuperAdmin")]
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] UpdateSessionCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest("ID in URL does not match ID in request body.");
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "CinemaManager,SuperAdmin")]
    public async Task<IActionResult> CancelSession(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelSessionCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("generate-schedule")]
    [Authorize(Roles = "SuperAdmin,CinemaManager")]
    public async Task<ActionResult<int>> GenerateSchedule([FromBody] GenerateScheduleCommand command, CancellationToken cancellationToken)
    {
        var createdCount = await _mediator.Send(command, cancellationToken);

        return Ok(new
        {
            Success = true,
            Count = createdCount,
            Message = $"Successfully generated {createdCount} sessions for the specified day."
        });
    }

    [HttpPut("move")]
    [Authorize(Roles = "SuperAdmin,CinemaManager")]
    public async Task<IActionResult> MoveSession([FromBody] MoveSessionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new { Success = result, Message = "Session successfully rescheduled." });
    }
}