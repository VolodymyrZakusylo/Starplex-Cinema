using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Models;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Infrastructure.Identity;
using StarPlex.Infrastructure.Authentication;
using StarPlex.Infrastructure.Persistence;

namespace StarPlex.Application.IntegrationTests.Features;

public class UserRoleUpdateTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Theory]
    [InlineData(UserRole.Cashier)]
    [InlineData(UserRole.CinemaManager)]
    public async Task StaffRole_RequiresExistingCinema(UserRole role)
    {
        var (user, cinema) = await SeedAsync();
        var service = CreateService();
        var missing = () => service.UpdateUserRoleAndCinemaAsync(user.Id, role, null);
        await missing.Should().ThrowAsync<BusinessRuleException>().WithMessage("*cinema must be specified*");
        var invalid = () => service.UpdateUserRoleAndCinemaAsync(user.Id, role, Guid.NewGuid());
        await invalid.Should().ThrowAsync<BusinessRuleException>().WithMessage("*does not exist*");
        await AssertAssignmentAsync(user.Id, UserRole.Cashier, cinema.Id);
    }

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task GlobalRole_ClearsCinema(UserRole role)
    {
        var (user, cinema) = await SeedAsync();
        (await CreateService().UpdateUserRoleAndCinemaAsync(user.Id, role, cinema.Id)).Should().BeTrue();
        await AssertAssignmentAsync(user.Id, role, null);
    }

    [Fact]
    public async Task UndefinedRole_IsRejectedWithoutMutations()
    {
        var (user, cinema) = await SeedAsync();
        var update = () => CreateService().UpdateUserRoleAndCinemaAsync(user.Id, (UserRole)99, cinema.Id);
        await update.Should().ThrowAsync<BusinessRuleException>();
        await AssertAssignmentAsync(user.Id, UserRole.Cashier, cinema.Id);
    }

    [Fact]
    public async Task StaffRole_UpdatesRoleAndCinemaTogether()
    {
        var (user, _) = await SeedAsync();
        var cinema = new Cinema("New Cinema", "Test", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        await DbContext.SaveChangesAsync(default);
        (await CreateService().UpdateUserRoleAndCinemaAsync(user.Id, UserRole.CinemaManager, cinema.Id)).Should().BeTrue();
        await AssertAssignmentAsync(user.Id, UserRole.CinemaManager, cinema.Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task FailedIdentityMutation_RollsBackRolesAndCinema(int failAt)
    {
        var (user, originalCinema) = await SeedAsync();
        var target = new Cinema("Target", "Test", "Kyiv");
        DbContext.Cinemas.Add(target);
        await DbContext.SaveChangesAsync(default);
        ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().UserValidators.Add(new RejectUpdate(failAt));

        var update = () => CreateService().UpdateUserRoleAndCinemaAsync(user.Id, UserRole.CinemaManager, target.Id);

        await update.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Simulated identity failure*");
        await AssertAssignmentAsync(user.Id, UserRole.Cashier, originalCinema.Id);
    }

    private IdentityService CreateService() => new(
        ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
        ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
        (ApplicationDbContext)DbContext, Options.Create(new JwtSettings()), NullLogger<IdentityService>.Instance);

    private async Task<(ApplicationUser User, Cinema Cinema)> SeedAsync()
    {
        var roles = ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Enum.GetNames<UserRole>())
            (await roles.CreateAsync(new IdentityRole<Guid>(role))).Succeeded.Should().BeTrue();
        var cinema = new Cinema("Original", "Test", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        await DbContext.SaveChangesAsync(default);
        var users = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "disposable@example.test", Email = "disposable@example.test", CinemaId = cinema.Id };
        (await users.CreateAsync(user)).Succeeded.Should().BeTrue();
        (await users.AddToRoleAsync(user, "Cashier")).Succeeded.Should().BeTrue();
        return (user, cinema);
    }

    private async Task AssertAssignmentAsync(Guid userId, UserRole expectedRole, Guid? cinemaId)
    {
        using var scope = ServiceProvider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByIdAsync(userId.ToString()))!;
        user.CinemaId.Should().Be(cinemaId);
        (await users.GetRolesAsync(user)).Should().Equal(expectedRole.ToString());
    }

    private sealed class RejectUpdate(int failAt) : IUserValidator<ApplicationUser>
    {
        private int _calls;
        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
            => Task.FromResult(++_calls == failAt
                ? IdentityResult.Failed(new IdentityError { Code = "Probe", Description = "Simulated identity failure" })
                : IdentityResult.Success);
    }
}
