using MediatR;
using StarPlex.Application.Features.Cinemas.DTOs;

namespace StarPlex.Application.Features.Cinemas.Queries.GetCinemas;

public class GetCinemasQuery : IRequest<List<CinemaDto>>
{
}