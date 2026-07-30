using MediatR;
using StarPlex.Application.Features.Sessions.DTOs;

namespace StarPlex.Application.Features.Sessions.Queries.GetSessionsByCinemaId;

public class GetSessionsByCinemaIdQuery : IRequest<List<SessionDto>>
{
    public Guid CinemaId { get; set; }
}