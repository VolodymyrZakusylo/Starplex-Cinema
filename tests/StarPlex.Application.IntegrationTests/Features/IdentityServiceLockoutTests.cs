using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Infrastructure.Authentication;
using StarPlex.Infrastructure.Identity;
using StarPlex.Infrastructure.Persistence;

namespace StarPlex.Application.IntegrationTests.Features;

public class IdentityServiceLockoutTests : IntegrationTestBase
{
    public IdentityServiceLockoutTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task LoginAsync_WithMultipleFailedAttempts_LocksOutAccount()
    {
        var service = CreateService();
        var email = "test_lockout@example.com";
        var password = "Password123!";
        var wrongPassword = "WrongPassword123!";
        
        await CreateUserAsync(email, password);


        for (int i = 0; i < 5; i++)
        {
            var response = await service.LoginAsync(email, wrongPassword);
            response.Should().BeNull();
        }


        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        
        user.Should().NotBeNull();
        var isLockedOut = await userManager.IsLockedOutAsync(user!);
        isLockedOut.Should().BeTrue();

        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user!);
        lockoutEnd.Should().NotBeNull();
        lockoutEnd!.Value.Should().BeAfter(DateTimeOffset.UtcNow);


        var lockedResponse = await service.LoginAsync(email, password);
        lockedResponse.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_ResetsFailedAccessCount()
    {
        var service = CreateService();
        var email = "test_reset_lockout@example.com";
        var password = "Password123!";
        var wrongPassword = "WrongPassword123!";
        
        await CreateUserAsync(email, password);


        for (int i = 0; i < 3; i++)
        {
            var response = await service.LoginAsync(email, wrongPassword);
            response.Should().BeNull();
        }

        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        var failedCount = await userManager.GetAccessFailedCountAsync(user!);
        failedCount.Should().Be(3);


        var successResponse = await service.LoginAsync(email, password);
        successResponse.Should().NotBeNull();


        var resetCount = await userManager.GetAccessFailedCountAsync(user!);
        resetCount.Should().Be(0);
    }

    private async Task CreateUserAsync(string email, string password)
    {
        var users = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser("Test", "User", DateTime.UtcNow.AddYears(-20)) { UserName = email, Email = email };
        await users.CreateAsync(user, password);
    }

    private IdentityService CreateService()
    {
        var jwtSettings = new JwtSettings 
        { 
            Secret = "a_very_long_secret_key_for_testing_purposes_only_1234567890",
            Issuer = "test",
            Audience = "test",
            ExpiryInMinutes = 60
        };

        return new IdentityService(
            ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            (ApplicationDbContext)DbContext, 
            Options.Create(jwtSettings), 
            NullLogger<IdentityService>.Instance);
    }
}
