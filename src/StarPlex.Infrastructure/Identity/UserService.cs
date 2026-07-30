using Microsoft.AspNetCore.Identity;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Infrastructure.Identity;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(string Email, string FullName)?> GetUserContactInfoAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        string fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrEmpty(fullName)) fullName = user.UserName ?? "Клієнт";

        return (user.Email ?? string.Empty, fullName);
    }
}