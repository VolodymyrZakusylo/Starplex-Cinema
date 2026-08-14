using Microsoft.EntityFrameworkCore;
using Npgsql;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;

namespace StarPlex.Infrastructure.Services;

public class PostgresSeatLockService : ISeatLockService
{
    private readonly IApplicationDbContext _context;

    public PostgresSeatLockService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> LockSeatsAsync(Guid sessionId, List<Guid> seatIds, Guid userId, CancellationToken ct)
    {
        if (_context is not DbContext dbContext) return false;

        var utcNow = DateTime.UtcNow;
        var lockExpiry = DateTime.SpecifyKind(utcNow.AddMinutes(10), DateTimeKind.Utc);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var alreadyTaken = await _context.BookingSeats
                .AnyAsync(bs => bs.Booking.SessionId == sessionId &&
                                seatIds.Contains(bs.SeatId) &&
                               (bs.Booking.Status == BookingStatus.Confirmed ||
                                bs.Booking.Status == BookingStatus.Pending), ct);

            if (alreadyTaken)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            var alreadyLocked = await _context.SelectedSeats
                .AnyAsync(ss => ss.SessionId == sessionId &&
                                seatIds.Contains(ss.SeatId) &&
                                ss.UserId != userId &&
                                ss.LockedUntil > utcNow, ct);

            if (alreadyLocked)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            var existingUserLocks = await _context.SelectedSeats
                .Where(ss => ss.SessionId == sessionId &&
                             seatIds.Contains(ss.SeatId) &&
                             ss.UserId == userId)
                .ToListAsync(ct);

            if (existingUserLocks.Any())
            {
                _context.SelectedSeats.RemoveRange(existingUserLocks);
            }

            var newLocks = seatIds.Select(seatId => new SelectedSeat(
                sessionId,
                seatId,
                userId,
                lockExpiry
            )
            { Id = Guid.NewGuid() }).ToList();

            await _context.SelectedSeats.AddRangeAsync(newLocks, ct);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            await transaction.RollbackAsync(ct);
            return false;
        }
    }

    public async Task ReleaseSeatLockAsync(Guid sessionId, Guid userId, CancellationToken ct)
    {
        var locks = await _context.SelectedSeats
            .Where(ss => ss.SessionId == sessionId &&
                         ss.UserId == userId)
            .ToListAsync(ct);

        if (locks.Any())
        {
            _context.SelectedSeats.RemoveRange(locks);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<List<Guid>> GetLockedSeatIdsAsync(Guid sessionId, CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;

        return await _context.SelectedSeats
            .AsNoTracking()
            .Where(ss => ss.SessionId == sessionId && ss.LockedUntil > utcNow)
            .Select(ss => ss.SeatId)
            .ToListAsync(ct);
    }
}