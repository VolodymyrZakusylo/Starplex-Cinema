using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Infrastructure.Identity;
using StarPlex.Infrastructure.Persistence;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Infrastructure;

[Collection("IntegrationTestCollection")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private IServiceScope _scope = null!;

    protected IApplicationDbContext DbContext { get; private set; } = null!;
    protected IMediator Mediator { get; private set; } = null!;
    protected IServiceProvider ServiceProvider => _scope.ServiceProvider;

    // Mocks for external dependencies
    protected Mock<IPaymentService> PaymentServiceMock { get; } = new();
    protected Mock<ISeatLockService> SeatLockServiceMock { get; } = new();
    protected Mock<ICurrentUserService> CurrentUserServiceMock { get; } = new();
    protected Mock<IUserService> UserServiceMock { get; } = new();
    protected Mock<ITicketService> TicketServiceMock { get; } = new();
    protected Mock<IEmailService> EmailServiceMock { get; } = new();
    protected Mock<ISeatHubService> SeatHubServiceMock { get; } = new();

    public IntegrationTestBase(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        // Reset database tables preserving __EFMigrationsHistory
        await _databaseFixture.ResetDatabaseAsync();

        var services = new ServiceCollection();

        // Register DbContext with shared PostgreSQL Testcontainer connection string
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(_databaseFixture.ConnectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IApplicationDbContext).Assembly));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Register Mocked Dependencies
        services.AddScoped(_ => PaymentServiceMock.Object);
        services.AddScoped(_ => SeatLockServiceMock.Object);
        services.AddScoped(_ => CurrentUserServiceMock.Object);
        services.AddScoped(_ => UserServiceMock.Object);
        services.AddScoped(_ => TicketServiceMock.Object);
        services.AddScoped(_ => EmailServiceMock.Object);
        services.AddScoped(_ => SeatHubServiceMock.Object);

        var provider = services.BuildServiceProvider();
        _scope = provider.CreateScope();

        DbContext = _scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        Mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        return Task.CompletedTask;
    }
}
