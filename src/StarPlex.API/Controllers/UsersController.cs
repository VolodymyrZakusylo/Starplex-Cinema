using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;
using StarPlex.Domain.Enums;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class UsersController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public UsersController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpGet("staff")]
    public async Task<ActionResult<PagedUserStaffResponse>> GetStaffDirectory(
        [FromQuery] string? searchTerm,
        [FromQuery] UserRole? roleFilter,
        [FromQuery] Guid? cinemaIdFilter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var (users, totalCount) = await _identityService.SearchUsersAsync(
            searchTerm ?? string.Empty,
            roleFilter,
            cinemaIdFilter,
            page,
            pageSize
        );

        return Ok(new PagedUserStaffResponse
        {
            Users = users,
            TotalCount = totalCount
        });
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(
        [FromRoute] Guid id,
        [FromBody] UpdateRoleRequest request)
    {
        var success = await _identityService.UpdateUserRoleAndCinemaAsync(id, request.NewRole, request.CinemaId);

            if (!success)
            {
                return BadRequest(new { message = "Failed to update user role. Please verify the User ID." });
            }

            return Ok(new { message = "User role and access permissions updated successfully." });
    }
}

public class UpdateRoleRequest
{
    public UserRole NewRole { get; set; }
    public Guid? CinemaId { get; set; }
}

public class PagedUserStaffResponse
{
    public List<UserStaffDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
}