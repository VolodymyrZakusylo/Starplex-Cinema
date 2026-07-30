using Microsoft.AspNetCore.Identity;

namespace StarPlex.Infrastructure.Identity;

public static class RoleSeeder
{
    public static async Task SeedDataAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        string[] roles = ["SuperAdmin", "CinemaManager", "Cashier", "Customer"];

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        var adminEmail = "admin1@starplex.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser != null)
        {
            await userManager.RemovePasswordAsync(adminUser);

            var resetResult = await userManager.AddPasswordAsync(adminUser, "Admin123!");

            if (resetResult.Succeeded)
            {
                System.Console.WriteLine("=========================================================");
                System.Console.WriteLine("=== ПАРОЛЬ АДМІНА УСПІШНО СКИДАНЕНО НА: Admin123! ===");
                System.Console.WriteLine("=========================================================");
            }
            else
            {
                System.Console.WriteLine("=== ПОМИЛКА ПРИ СКИДАННІ ПАРОЛЯ АДМІНА ===");
                foreach (var error in resetResult.Errors)
                {
                    System.Console.WriteLine($"- {error.Description}");
                }
            }
        }

        if (adminUser == null)
        {
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

            var createResult = await userManager.CreateAsync(defaultAdmin, "Admin123!");

            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(defaultAdmin, "SuperAdmin");
            }
        }
    }
}