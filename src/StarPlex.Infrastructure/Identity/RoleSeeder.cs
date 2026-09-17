using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StarPlex.Infrastructure.Identity;

public static class RoleSeeder
{
    public static async Task SeedDataAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration? configuration = null,
        ILogger? logger = null)
    {
        string[] roles = ["SuperAdmin", "CinemaManager", "Cashier", "Customer"];

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleCreateResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!roleCreateResult.Succeeded)
                {
                    logger?.LogWarning("Failed to create required role {RoleName}.", roleName);
                }
            }
        }

        string? adminEmail = configuration?["SeedSettings:AdminEmail"]?.Trim();

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            logger?.LogWarning("Administrator email is not configured in SeedSettings:AdminEmail. Administrator seeding skipped.");
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser != null)
        {
            logger?.LogInformation("Administrator account already exists.");

            if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                if (roleResult.Succeeded)
                {
                    logger?.LogInformation("Added SuperAdmin role to existing administrator account.");
                }
                else
                {
                    logger?.LogWarning("Failed to add SuperAdmin role to existing administrator account.");
                }
            }

            return;
        }

        string? adminPassword = configuration?["SeedSettings:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger?.LogWarning("Administrator account does not exist and SeedSettings:AdminPassword is not configured. Administrator creation skipped.");
            return;
        }

        var defaultAdmin = new ApplicationUser(
            firstName: "System",
            lastName: "Administrator",
            dateOfBirth: new DateTime(1990, 1, 1)
        )
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(defaultAdmin, adminPassword);

        if (createResult.Succeeded)
        {
            var roleResult = await userManager.AddToRoleAsync(defaultAdmin, "SuperAdmin");
            if (roleResult.Succeeded)
            {
                logger?.LogInformation("Administrator account created successfully.");
            }
            else
            {
                logger?.LogWarning("Administrator account created, but assigning SuperAdmin role failed.");
            }
        }
        else
        {
            logger?.LogWarning("Failed to create administrator account.");
        }
    }
}