using MediatR;
using StarPlex.Application.Features.Sessions.DTOs;

namespace StarPlex.Application.Features.Sessions.Queries.GetSessions;

public class GetSessionsQuery : IRequest<List<SessionDto>>
{
    public Guid? MovieId { get; set; }
    public DateTime? Date { get; set; }
}