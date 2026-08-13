using StarPlex.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Halls.Commands.CreateHall;

public class CreateHallCommandHandler : IRequestHandler<CreateHallCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateHallCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateHallCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsSuperAdmin && _currentUserService.CinemaId != request.CinemaId)
        {
            throw new ForbiddenException("You do not have permission to manage this cinema.");
        }

        var cinemaExists = await _context.Cinemas
            .AnyAsync(c => c.Id == request.CinemaId, cancellationToken);

        if (!cinemaExists)
        {
            throw new NotFoundException("Cinema", request.CinemaId);
        }

        var trimmedName = request.Name.Trim();

        var hallExists = await _context.Halls
            .AnyAsync(h => h.CinemaId == request.CinemaId && h.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (hallExists)
        {
            throw new ConflictException($"Hall with name '{trimmedName}' already exists in this cinema.");
        }

        var hall = new Hall(
            request.CinemaId,
            trimmedName,
            request.TotalRows,
            request.SeatsPerRow
        );

        _context.Halls.Add(hall);

        var seatsToCreate = new List<Seat>();

        for (int row = 1; row <= request.TotalRows; row++)
        {
            for (int seatNum = 1; seatNum <= request.SeatsPerRow; seatNum++)
            {
                SeatType assignedType = SeatType.Standard;

                if (row == request.TotalRows)
                {
                    assignedType = SeatType.VIP;
                }
                else if (row == 1 && (seatNum <= 2 || seatNum > request.SeatsPerRow - 2))
                {
                    assignedType = SeatType.Disabled;
                }

                var seat = new Seat(
                    hall.Id,
                    row.ToString(),
                    seatNum,
                    assignedType
                );

                seatsToCreate.Add(seat);
            }
        }

        _context.Seats.AddRange(seatsToCreate);
        await _context.SaveChangesAsync(cancellationToken);

        return hall.Id;
    }
}