using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Features.Users.Commands.ChangePassword;
using StarPlex.Application.Features.Users.Commands.DeleteAccount;
using StarPlex.Application.Features.Users.Commands.UpdateProfile;
using StarPlex.Application.Common.Exceptions;
using System.Security.Claims;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();

        var success = await _mediator.Send(new UpdateProfileCommand(userId, request.FirstName, request.LastName));

        if (!success)
            return BadRequest(new { Message = "Failed to update profile data. Ensure all fields are filled." });

        return Ok(new { Message = "Profile updated successfully." });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();

        var success = await _mediator.Send(new ChangePasswordCommand(userId, request.OldPassword, request.NewPassword));

        if (!success)
            return BadRequest(new { Message = "Invalid current password or new password requirements failed." });

        return Ok(new { Message = "Password changed successfully." });
    }

    [HttpDelete("delete-account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetCurrentUserId();

        var success = await _mediator.Send(new DeleteAccountCommand(userId));

        if (!success)
            return BadRequest(new { Message = "Failed to delete account." });

        return Ok(new { Message = "Account successfully deleted." });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("User identification missing from token.");
        }
        return userId;
    }
}