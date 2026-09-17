using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private IServiceScope _scope = null!;
    
    protected IApplicationDbContext DbContext { get; private set; } = null!;
    protected IMediator Mediator { get; private set; } = null!;
    
    // Mocks for external dependencies
    protected Mock<IPaymentService> PaymentServiceMock { get; } = new();
    protected Mock<ISeatLockService> SeatLockServiceMock { get; } = new();
    protected Mock<ICurrentUserService> CurrentUserServiceMock { get; } = new();
    protected Mock<IUserService> UserServiceMock { get; } = new();
    protected Mock<ITicketService> TicketServiceMock { get; } = new();
    protected Mock<IEmailService> EmailServiceMock { get; } = new();
    protected Mock<ISeatHubService> SeatHubServiceMock { get; } = new();

    public IntegrationTestBase()
    {
        _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase("starplex_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var services = new ServiceCollection();

        // 1. Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IApplicationDbContext).Assembly));

        // 3. Register Mocked Dependencies
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

        var db = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        await _dbContainer.DisposeAsync();
    }
}
