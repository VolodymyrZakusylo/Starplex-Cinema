using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

    public Guid? CinemaId
    {
        get
        {
            var cinemaIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst("cinemaId")?.Value;
            return Guid.TryParse(cinemaIdStr, out var id) ? id : null;
        }
    }

    public bool IsSuperAdmin => Role == "SuperAdmin";
}