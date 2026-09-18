using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StarPlex.Infrastructure.Identity;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Infrastructure;

public class RoleSeederTests : IntegrationTestBase
{
    public RoleSeederTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private RoleManager<IdentityRole<Guid>> RoleManager => ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    private UserManager<ApplicationUser> UserManager => ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    [Fact]
    public async Task SeedDataAsync_FirstSeed_CreatesRolesAndAdminWithRole()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SeedSettings:AdminEmail", "admin_first@starplex.com" },
                { "SeedSettings:AdminPassword", "AdminPass123!" }
            })
            .Build();

        await RoleSeeder.SeedDataAsync(RoleManager, UserManager, config);

        (await RoleManager.RoleExistsAsync("SuperAdmin")).Should().BeTrue();
        (await RoleManager.RoleExistsAsync("CinemaManager")).Should().BeTrue();
        (await RoleManager.RoleExistsAsync("Cashier")).Should().BeTrue();
        (await RoleManager.RoleExistsAsync("Customer")).Should().BeTrue();

        var admin = await UserManager.FindByEmailAsync("admin_first@starplex.com");
        admin.Should().NotBeNull();
        (await UserManager.IsInRoleAsync(admin!, "SuperAdmin")).Should().BeTrue();
    }

    [Fact]
    public async Task SeedDataAsync_RepeatedSeed_PreservesExistingAdminPasswordHash()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SeedSettings:AdminEmail", "admin_repeat@starplex.com" },
                { "SeedSettings:AdminPassword", "InitialPass123!" }
            })
            .Build();

        await RoleSeeder.SeedDataAsync(RoleManager, UserManager, config);

        var adminFirstRun = await UserManager.FindByEmailAsync("admin_repeat@starplex.com");
        adminFirstRun.Should().NotBeNull();
        string initialHash = adminFirstRun!.PasswordHash!;

        var differentPassConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SeedSettings:AdminEmail", "admin_repeat@starplex.com" },
                { "SeedSettings:AdminPassword", "DifferentPass123!" }
            })
            .Build();

        await RoleSeeder.SeedDataAsync(RoleManager, UserManager, differentPassConfig);

        var adminSecondRun = await UserManager.FindByEmailAsync("admin_repeat@starplex.com");
        adminSecondRun!.PasswordHash.Should().Be(initialHash);
    }

    [Fact]
    public async Task SeedDataAsync_ExistingAdmin_DoesNotRequireAdminPasswordConfig()
    {
        var initialConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SeedSettings:AdminEmail", "admin_nopass@starplex.com" },
                { "SeedSettings:AdminPassword", "InitialPass123!" }
            })
            .Build();

        await RoleSeeder.SeedDataAsync(RoleManager, UserManager, initialConfig);

        var admin = await UserManager.FindByEmailAsync("admin_nopass@starplex.com");
        string initialHash = admin!.PasswordHash!;

        var noPasswordConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SeedSettings:AdminEmail", "admin_nopass@starplex.com" }
            })
            .Build();

        await RoleSeeder.SeedDataAsync(RoleManager, UserManager, noPasswordConfig);

        var adminAfterSecondSeed = await UserManager.FindByEmailAsync("admin_nopass@starplex.com");
        adminAfterSecondSeed!.PasswordHash.Should().Be(initialHash);
        (await UserManager.IsInRoleAsync(adminAfterSecondSeed, "SuperAdmin")).Should().BeTrue();
    }

    [Fact]
    public async Task SeedDataAsync_MissingAdmin_WithoutPasswordConfig_SkipsAdminCreationSafely()
    {
        var configNoPassword = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SeedSettings:AdminEmail", "missing_admin@starplex.com" }
            })
            .Build();

        await RoleSeeder.SeedDataAsync(RoleManager, UserManager, configNoPassword);

        var admin = await UserManager.FindByEmailAsync("missing_admin@starplex.com");
        admin.Should().BeNull();
    }

    [Fact]
    public async Task SeedDataAsync_WithoutConfiguration_DoesNotCreateFallbackAdmin()
    {
        await RoleSeeder.SeedDataAsync(RoleManager, UserManager, configuration: null);

        (await RoleManager.RoleExistsAsync("SuperAdmin")).Should().BeTrue();

        var fallbackAdmin = await UserManager.FindByEmailAsync("admin1@starplex.com");
        fallbackAdmin.Should().BeNull();
    }

    [Fact]
    public async Task SeedDataAsync_ExistingAdminMissingRole_RepairsRoleMembershipWithoutModifyingPasswordOrProfile()
    {
        var customAdmin = new ApplicationUser(
            firstName: "CustomFirstName",
            lastName: "CustomLastName",
            dateOfBirth: new DateTime(1985, 5, 5)
        )
        {
            UserName = "custom_admin@starplex.com",
            Email = "custom_admin@starplex.com",
            EmailConfirmed = true
        };

        var createResult = await UserManager.CreateAsync(customAdmin, "CustomPass123!");
        createResult.Succeeded.Should().BeTrue();

        var createdUser = await UserManager.FindByEmailAsync("custom_admin@starplex.com");
        string expectedHash = createdUser!.PasswordHash!;
        (await UserManager.IsInRoleAsync(createdUser, "SuperAdmin")).Should().BeFalse();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SeedSettings:AdminEmail", "custom_admin@starplex.com" },
                { "SeedSettings:AdminPassword", "SomeOtherPass123!" }
            })
            .Build();

        await RoleSeeder.SeedDataAsync(RoleManager, UserManager, config);

        var seededUser = await UserManager.FindByEmailAsync("custom_admin@starplex.com");
        seededUser!.FirstName.Should().Be("CustomFirstName");
        seededUser.LastName.Should().Be("CustomLastName");
        seededUser.PasswordHash.Should().Be(expectedHash);

        (await UserManager.IsInRoleAsync(seededUser, "SuperAdmin")).Should().BeTrue();
    }

    [Fact]
    public async Task SeedDataAsync_NeverLogsSecretValues()
    {
        var secretPassword = "SuperSecretPassword123!";
        var loggerMock = new Mock<ILogger>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SeedSettings:AdminEmail", "secret_admin@starplex.com" },
                { "SeedSettings:AdminPassword", secretPassword }
            })
            .Build();

        await RoleSeeder.SeedDataAsync(RoleManager, UserManager, config, loggerMock.Object);

        loggerMock.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(secretPassword)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
