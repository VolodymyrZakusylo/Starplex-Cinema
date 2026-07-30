using MediatR;
using StarPlex.Application.Features.Halls.DTOs;

namespace StarPlex.Application.Features.Halls.Queries.GetHallsByCinemaId;

public class GetHallsByCinemaIdQuery : IRequest<List<HallDto>>
{
    public Guid CinemaId { get; set; }
}