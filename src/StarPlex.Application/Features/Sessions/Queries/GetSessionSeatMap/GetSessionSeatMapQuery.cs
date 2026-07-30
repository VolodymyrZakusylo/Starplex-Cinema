using MediatR;
using StarPlex.Application.Features.Sessions.DTOs;

namespace StarPlex.Application.Features.Sessions.Queries.GetSessionSeatMap;

public class GetSessionSeatMapQuery : IRequest<List<SeatMapDto>>
{
    public Guid SessionId { get; set; }
}