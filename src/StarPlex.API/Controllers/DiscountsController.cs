using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Features.Discounts.Commands.ApplyPromoCode;
using StarPlex.Application.Features.Discounts.Commands.CreatePromoCode;
using StarPlex.Application.Features.Discounts.Commands.DeletePromoCode;
using StarPlex.Application.Features.Discounts.Queries.GetAllPromoCodes;
using StarPlex.Application.Features.Discounts.Queries.GetPromoCodeByCode;
using StarPlex.Domain.Entities;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiscountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiscountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("validate/{code}")]
    public async Task<ActionResult<PromoCodeValidationResultDto>> ValidatePromoCode(string code)
    {
        var result = await _mediator.Send(new GetPromoCodeByCodeQuery(code));

        if (!result.IsValid)
        {
            return BadRequest(new { Message = result.Message });
        }

        return Ok(result);
    }

    [HttpPost("apply")]
    public async Task<ActionResult<PromoCodeResultDto>> ApplyPromoCode([FromBody] ApplyPromoCodeCommand command)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "User ID could not be determined." });

        command.UserId = userId.Value;
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { Message = result.Message });
        }

        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<Discount>>> GetAllPromoCodes()
    {
        var result = await _mediator.Send(new GetAllPromoCodesQuery());
        return Ok(result);
    }

    [HttpPost("create")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<Guid>> CreatePromoCode([FromBody] CreatePromoCodeCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { Id = id, IsSuccess = true });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeletePromoCode(Guid id)
    {
        await _mediator.Send(new DeletePromoCodeCommand(id));
        return NoContent();
    }
}