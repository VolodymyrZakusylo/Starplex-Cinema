using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StarPlex.Application.Common.Models;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Infrastructure.Authentication;
using StarPlex.Infrastructure.Identity;
using StarPlex.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace StarPlex.Application.IntegrationTests.Features;

public class RefreshTokenTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Login_ReturnsRawToken_AndPersistsHashedToken()
    {
        var service = CreateService();
        var email = "test_refresh@example.com";
        var password = "Password123!";
        
        await CreateUserAsync(email, password);
        var response = await service.LoginAsync(email, password);

        response.Should().NotBeNull();
        response!.RefreshToken.Should().NotBeNullOrEmpty();

        var dbContext = (ApplicationDbContext)DbContext;
        var rawTokenExists = await dbContext.RefreshTokens.AsNoTracking().AnyAsync(r => r.Token == response.RefreshToken);
        rawTokenExists.Should().BeFalse();

        var expectedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(response.RefreshToken)));
        var hashedTokenRecord = await dbContext.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(r => r.Token == expectedHash);
        
        hashedTokenRecord.Should().NotBeNull();
        hashedTokenRecord!.UserId.Should().Be(response.UserId);
        hashedTokenRecord.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshToken_WithValidRawToken_RotatesSuccessfully()
    {
        var service = CreateService();
        var email = "test_rotate@example.com";
        var password = "Password123!";
        
        await CreateUserAsync(email, password);
        var loginResponse = await service.LoginAsync(email, password);
        
        var originalRawToken = loginResponse!.RefreshToken;
        var originalHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(originalRawToken)));

        var refreshResponse = await service.RefreshTokenAsync(originalRawToken);

        refreshResponse.Should().NotBeNull();
        refreshResponse!.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResponse.RefreshToken.Should().NotBe(originalRawToken);

        var dbContext = (ApplicationDbContext)DbContext;
        var originalTokenRecord = await dbContext.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(r => r.Token == originalHash);
        originalTokenRecord.Should().NotBeNull();
        originalTokenRecord!.IsRevoked.Should().BeTrue();

        var newHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshResponse.RefreshToken)));
        var newTokenRecord = await dbContext.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(r => r.Token == newHash);
        newTokenRecord.Should().NotBeNull();
        newTokenRecord!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshToken_WithInvalidOrRevokedRawToken_Fails()
    {
        var service = CreateService();
        var email = "test_invalid@example.com";
        var password = "Password123!";
        
        await CreateUserAsync(email, password);
        var loginResponse = await service.LoginAsync(email, password);
        
        var originalRawToken = loginResponse!.RefreshToken;

        await service.RevokeTokenAsync(originalRawToken);

        var refreshResponse = await service.RefreshTokenAsync(originalRawToken);

        refreshResponse.Should().BeNull();
        
        var invalidRefreshResponse = await service.RefreshTokenAsync("invalid-random-token");
        invalidRefreshResponse.Should().BeNull();
    }

    [Fact]
    public async Task RevokeToken_WithValidRawToken_RevokesHashedRecord()
    {
        var service = CreateService();
        var email = "test_revoke@example.com";
        var password = "Password123!";
        
        await CreateUserAsync(email, password);
        var loginResponse = await service.LoginAsync(email, password);
        
        var originalRawToken = loginResponse!.RefreshToken;
        var originalHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(originalRawToken)));

        var result = await service.RevokeTokenAsync(originalRawToken);

        result.Should().BeTrue();

        var dbContext = (ApplicationDbContext)DbContext;
        var tokenRecord = await dbContext.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(r => r.Token == originalHash);
        tokenRecord.Should().NotBeNull();
        tokenRecord!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshToken_Replay_ReturnsNull()
    {
        var service = CreateService();
        var email = "test_replay@example.com";
        var password = "Password123!";
        
        await CreateUserAsync(email, password);
        var loginResponse = await service.LoginAsync(email, password);
        
        var originalRawToken = loginResponse!.RefreshToken;

        var firstRefresh = await service.RefreshTokenAsync(originalRawToken);
        firstRefresh.Should().NotBeNull();

        var secondRefresh = await service.RefreshTokenAsync(originalRawToken);
        secondRefresh.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_Concurrent_OnlyOneSucceeds()
    {
        var service1 = CreateService();
        var email = "test_concurrent@example.com";
        var password = "Password123!";
        
        await CreateUserAsync(email, password);
        var loginResponse = await service1.LoginAsync(email, password);
        var originalRawToken = loginResponse!.RefreshToken;

        using var scope1 = ServiceProvider.CreateScope();
        using var scope2 = ServiceProvider.CreateScope();

        var jwtSettings = new JwtSettings { Secret = "a_very_long_secret_key_for_testing_purposes_only_1234567890", Issuer = "test", Audience = "test", ExpiryInMinutes = 60 };
        
        var concurrentService1 = new IdentityService(
            scope1.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope1.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            Options.Create(jwtSettings),
            NullLogger<IdentityService>.Instance);

        var concurrentService2 = new IdentityService(
            scope2.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope2.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            Options.Create(jwtSettings),
            NullLogger<IdentityService>.Instance);

        var task1 = concurrentService1.RefreshTokenAsync(originalRawToken);
        var task2 = concurrentService2.RefreshTokenAsync(originalRawToken);

        var results = await Task.WhenAll(task1, task2);

        var successfulRefreshes = results.Count(r => r != null);
        var failedRefreshes = results.Count(r => r == null);

        successfulRefreshes.Should().Be(1);
        failedRefreshes.Should().Be(1);

        var dbContext = (ApplicationDbContext)DbContext;
        var activeTokensCount = await dbContext.RefreshTokens.AsNoTracking().CountAsync(r => r.UserId == loginResponse.UserId && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow);
        activeTokensCount.Should().Be(1);
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
