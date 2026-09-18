using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StarPlex.Domain.Entities;

namespace StarPlex.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Cinema> Cinemas { get; }
    DbSet<Hall> Halls { get; }
    DbSet<Seat> Seats { get; }
    DbSet<Movie> Movies { get; }
    DbSet<Session> Sessions { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<BookingSeat> BookingSeats { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<Payment> Payments { get; }
    DbSet<SelectedSeat> SelectedSeats { get; }
    DbSet<Discount> Discounts { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> HasUsersAssignedToCinemaAsync(Guid cinemaId, CancellationToken cancellationToken);
    bool IsForeignKeyViolation(DbUpdateException exception, string constraintName);
}
