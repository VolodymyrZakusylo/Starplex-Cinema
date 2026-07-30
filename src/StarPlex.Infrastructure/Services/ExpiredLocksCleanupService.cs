using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Infrastructure.Services;

public class ExpiredLocksCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredLocksCleanupService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

    public ExpiredLocksCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredLocksCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired Locks and Bookings Cleanup Service has started.");

        using var timer = new PeriodicTimer(_period);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanExpiredLocksAndBookingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning expired seat locks or pending bookings.");
            }
        }
    }

    private async Task CleanExpiredLocksAndBookingsAsync(CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var seatHubService = scope.ServiceProvider.GetRequiredService<ISeatHubService>();

        bool changesMade = false;

        var expiredLocks = await context.SelectedSeats
            .Where(ss => ss.LockedUntil <= utcNow)
            .ToListAsync(ct);

        if (expiredLocks.Any())
        {
            _logger.LogInformation("Found {Count} expired seat locks. Cleaning up...", expiredLocks.Count);

            var groupedBySession = expiredLocks.GroupBy(l => l.SessionId);

            context.SelectedSeats.RemoveRange(expiredLocks);
            changesMade = true;

            foreach (var sessionGroup in groupedBySession)
            {
                var releasedSeatIds = sessionGroup.Select(l => l.SeatId).ToList();
                await seatHubService.NotifySeatsReleasedAsync(sessionGroup.Key, releasedSeatIds, ct);
            }
        }

        var bookingThreshold = utcNow.AddMinutes(-15);

        var expiredPendingBookings = await context.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.BookingTime <= bookingThreshold)
            .ToListAsync(ct);

        if (expiredPendingBookings.Any())
        {
            _logger.LogInformation("Found {Count} expired pending bookings. Changing status to Cancelled...", expiredPendingBookings.Count);

            foreach (var booking in expiredPendingBookings)
            {
                booking.Status = BookingStatus.Cancelled;
            }

            changesMade = true;
        }

        if (changesMade)
        {
            await context.SaveChangesAsync(ct);
        }
    }
}