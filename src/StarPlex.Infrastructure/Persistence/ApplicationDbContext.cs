using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StarPlex.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Cinema> Cinemas { get; set; } = null!;
    public DbSet<Hall> Halls { get; set; } = null!;
    public DbSet<Seat> Seats { get; set; } = null!;
    public DbSet<Movie> Movies { get; set; } = null!;
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;
    public DbSet<BookingSeat> BookingSeats { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<SelectedSeat> SelectedSeats { get; set; } = null!;
    public DbSet<Discount> Discounts { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        GenerateAuditLogs();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void GenerateAuditLogs()
    {
        ChangeTracker.DetectChanges();
        var logsToAdd = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged || entry.Entity is AuditLog)
                continue;

            var entityName = entry.Entity.GetType().Name;

            bool shouldLog = entityName switch
            {
                "Cinema" => true,
                "Hall" => true,
                "Session" => true,
                "Booking" => entry.State == EntityState.Modified,
                _ => false
            };

            if (!shouldLog)
                continue;

            var entityIdProperty = entry.Property("Id");
            var entityId = entityIdProperty != null && entityIdProperty.CurrentValue is Guid guidId ? guidId : Guid.Empty;

            var userId = _currentUserService.UserId ?? Guid.Empty;
            string actionDescription = entry.State.ToString();

            if (entityName == "Booking" && entry.State == EntityState.Modified)
            {
                var statusProperty = entry.Property("Status");
                if (statusProperty != null && statusProperty.CurrentValue?.ToString() == "Cancelled")
                {
                    actionDescription = "TicketRefund / BookingCancelled";
                }
            }

            logsToAdd.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = actionDescription,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow
            });
        }

        if (logsToAdd.Any())
        {
            AuditLogs.AddRange(logsToAdd);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<ApplicationUser>(b => b.ToTable("Users"));
        modelBuilder.Entity<IdentityRole<Guid>>(b => b.ToTable("Roles"));
        modelBuilder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("UserRoles"));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("UserClaims"));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("UserLogins"));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("RoleClaims"));
        modelBuilder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("UserTokens"));

        modelBuilder.Entity<SelectedSeat>(entity =>
        {
            entity.Property(s => s.CreatedAt)
                .HasDefaultValueSql("timezone('utc', now())");
        });
    }
}