using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Infrastructure.Services;

public class AuditLogCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditLogCleanupService> _logger;

    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);
    private readonly int _retentionDays = 30;

    public AuditLogCleanupService(IServiceProvider serviceProvider, ILogger<AuditLogCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit Log Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting automated audit log cleanup...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

                    var oldLogs = await context.AuditLogs
                        .Where(log => log.Timestamp < cutoffDate)
                        .ToListAsync(stoppingToken);

                    if (oldLogs.Any())
                    {
                        _logger.LogInformation("Found {Count} audit logs older than {Days} days. Deleting...", oldLogs.Count, _retentionDays);

                        context.AuditLogs.RemoveRange(oldLogs);
                        await context.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Successfully truncated expired audit records.");
                    }
                    else
                    {
                        _logger.LogInformation("No expired audit logs found.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during audit log cleanup.");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Audit Log Cleanup Service is stopping.");
    }
}