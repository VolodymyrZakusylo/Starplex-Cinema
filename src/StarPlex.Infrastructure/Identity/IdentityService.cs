using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Infrastructure.Authentication;
using StarPlex.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StarPlex.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<bool> IsEmailUniqueAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user == null;
    }

    public async Task<AuthResponse?> RegisterAsync(string email, string password, string firstName, string lastName, DateTime dateOfBirth, CancellationToken cancellationToken = default)
    {
        if (!await IsEmailUniqueAsync(email))
            return null;

        var user = new ApplicationUser(firstName, lastName, dateOfBirth)
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return null;

        await _userManager.AddToRoleAsync(user, UserRole.Customer.ToString());

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return null;

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid) return null;

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshTokenStr, CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshTokenStr, cancellationToken);

        if (storedToken == null || !storedToken.IsActive)
            return null;

        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync(cancellationToken);

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user == null) return null;

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<bool> RevokeTokenAsync(string refreshTokenStr, CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshTokenStr, cancellationToken);

        if (storedToken == null) return false;

        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, string firstName, string lastName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
        return result.Succeeded;
    }

    public async Task<bool> DeleteAccountAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<(List<UserStaffDto> Users, int TotalCount)> SearchUsersAsync(
        string searchTerm,
        UserRole? roleFilter,
        Guid? cinemaIdFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerTerm = searchTerm.ToLower().Trim();
            query = query.Where(u => u.Email!.ToLower().Contains(lowerTerm) ||
                                     u.FirstName.ToLower().Contains(lowerTerm) ||
                                     u.LastName.ToLower().Contains(lowerTerm));
        }

        if (cinemaIdFilter.HasValue)
        {
            query = query.Where(u => u.CinemaId == cinemaIdFilter.Value);
        }

        if (roleFilter.HasValue)
        {
            string roleName = roleFilter.Value.ToString();
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var userIdsInRole = _context.UserRoles
                    .Where(ur => ur.RoleId == role.Id)
                    .Select(ur => ur.UserId);

                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var usersDtoQuery = from user in query
                            join cinema in _context.Cinemas on user.CinemaId equals cinema.Id into cinemaJoin
                            from subCinema in cinemaJoin.DefaultIfEmpty()
                            select new UserStaffDto
                            {
                                Id = user.Id,
                                Email = user.Email ?? string.Empty,
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                CinemaId = user.CinemaId,
                                CinemaName = subCinema != null ? $"{subCinema.City} — {subCinema.Name}" : "AllCinemas",
                                CurrentRole = string.Empty
                            };

        var pagedUsers = await usersDtoQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = pagedUsers.Select(u => u.Id).ToList();

        var userRolesMap = await (
            from ur in _context.UserRoles
            join role in _context.Roles on ur.RoleId equals role.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, role.Name }
        ).ToListAsync(cancellationToken);

        foreach (var dto in pagedUsers)
        {
            var matchedRole = userRolesMap.FirstOrDefault(r => r.UserId == dto.Id);
            dto.CurrentRole = matchedRole != null ? matchedRole.Name : UserRole.Customer.ToString();
        }

        return (pagedUsers, totalCount);
    }

    public async Task<bool> UpdateUserRoleAndCinemaAsync(Guid userId, UserRole newRole, Guid? cinemaId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        string roleName = newRole.ToString();

        if (newRole == UserRole.SuperAdmin || newRole == UserRole.Customer)
        {
            user.CinemaId = null;
        }
        else
        {
            if (!cinemaId.HasValue)
            {
                throw new BusinessRuleException("A cinema must be specified for cinema staff.");
            }
            user.CinemaId = cinemaId.Value;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) return false;

        var currentRoles = await _userManager.GetRolesAsync(user);

        if (currentRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        return roleResult.Succeeded;
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var userRoles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        if (user.CinemaId.HasValue)
        {
            claims.Add(new Claim("cinemaId", user.CinemaId.Value.ToString()));
        }

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes);

        var token = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiry,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(token);
        var accessTokenStr = tokenHandler.WriteToken(securityToken);

        var refreshTokenStr = GenerateSecureRefreshTokenString();

        var refreshTokenEntity = new RefreshToken(user.Id, refreshTokenStr, DateTime.UtcNow.AddDays(7));

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Token = accessTokenStr,
            RefreshToken = refreshTokenStr,
            TokenExpiry = expiry,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = userRoles,
            CinemaId = user.CinemaId
        };
    }

    private static string GenerateSecureRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}