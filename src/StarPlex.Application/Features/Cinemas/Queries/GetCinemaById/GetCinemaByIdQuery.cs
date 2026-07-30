using MediatR;
using StarPlex.Application.Features.Cinemas.DTOs;

namespace StarPlex.Application.Features.Cinemas.Queries.GetCinemaById;

public class GetCinemaByIdQuery : IRequest<CinemaDto?>
{
    public Guid Id { get; set; }
}