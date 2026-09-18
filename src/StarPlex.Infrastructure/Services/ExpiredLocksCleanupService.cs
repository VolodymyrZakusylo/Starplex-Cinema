using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;
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

    internal async Task CleanExpiredLocksAndBookingsAsync(CancellationToken ct = default)
    {
        var utcNow = DateTime.UtcNow;

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var seatHubService = scope.ServiceProvider.GetRequiredService<ISeatHubService>();

            var expiredLocks = await context.SelectedSeats
                .Where(ss => ss.LockedUntil <= utcNow)
                .ToListAsync(ct);

            if (expiredLocks.Any())
            {
                _logger.LogInformation("Found {Count} expired seat locks. Cleaning up...", expiredLocks.Count);

                var groupedBySession = expiredLocks.GroupBy(l => l.SessionId);

                context.SelectedSeats.RemoveRange(expiredLocks);
                await context.SaveChangesAsync(ct);

                foreach (var sessionGroup in groupedBySession)
                {
                    var releasedSeatIds = sessionGroup.Select(l => l.SeatId).ToList();
                    await seatHubService.NotifySeatsReleasedAsync(sessionGroup.Key, releasedSeatIds, ct);
                }
            }
        }

        var bookingThreshold = utcNow.AddMinutes(-15);

        List<Guid> candidateBookingIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            candidateBookingIds = await context.Bookings
                .AsNoTracking()
                .Where(b => b.Status == BookingStatus.Pending && b.BookingTime <= bookingThreshold)
                .Select(b => b.Id)
                .ToListAsync(ct);
        }

        if (candidateBookingIds.Any())
        {
            _logger.LogInformation("Found {Count} candidate expired pending bookings. Checking for cancellation...", candidateBookingIds.Count);

            foreach (var bookingId in candidateBookingIds)
            {
                using var bookingScope = _scopeFactory.CreateScope();
                var bookingDbContext = bookingScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                if (bookingDbContext is not DbContext dbContext) continue;

                await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

                if (dbContext.Database.ProviderName?.Contains("Npgsql") == true)
                {
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT 1 FROM \"Bookings\" WHERE \"Id\" = {bookingId} FOR UPDATE", ct);
                }

                var booking = await bookingDbContext.Bookings
                    .Include(b => b.BookingSeats)
                    .Include(b => b.Payment)
                    .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

                if (booking == null)
                {
                    await transaction.RollbackAsync(ct);
                    continue;
                }

                if (booking.Status != BookingStatus.Pending || booking.BookingTime > bookingThreshold)
                {
                    _logger.LogInformation("Booking {BookingId} status is '{Status}' or no longer expired; skipping cancellation.", bookingId, booking.Status);
                    await transaction.RollbackAsync(ct);
                    continue;
                }

                if (booking.Payment == null || string.IsNullOrWhiteSpace(booking.Payment.StripePaymentIntentId))
                {
                    _logger.LogWarning("Cannot safely expire booking {BookingId} without a persisted PaymentIntent ID; leaving pending for retry.", bookingId);
                    await transaction.RollbackAsync(ct);
                    continue;
                }

                var paymentService = bookingScope.ServiceProvider.GetRequiredService<IPaymentService>();
                var expiryResult = await paymentService.ExpirePaymentIntentAsync(booking.Payment.StripePaymentIntentId, ct);
                if (expiryResult != PaymentIntentExpiryResult.Cancelled)
                {
                    _logger.LogWarning("PaymentIntent expiry for booking {BookingId} returned {Result}; leaving pending for confirmation or retry.", bookingId, expiryResult);
                    await transaction.RollbackAsync(ct);
                    continue;
                }

                booking.Status = BookingStatus.Cancelled;

                var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
                if (seatIds.Any())
                {
                    var temporaryLocks = await bookingDbContext.SelectedSeats
                        .Where(ss => ss.SessionId == booking.SessionId &&
                                     seatIds.Contains(ss.SeatId) &&
                                     ss.UserId == booking.UserId)
                        .ToListAsync(ct);

                    if (temporaryLocks.Any())
                    {
                        bookingDbContext.SelectedSeats.RemoveRange(temporaryLocks);
                    }
                }

                await bookingDbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Expired pending booking {BookingId} successfully cancelled.", bookingId);
            }
        }
    }
}
