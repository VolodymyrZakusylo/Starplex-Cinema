using MediatR;
using StarPlex.Application.Features.Halls.DTOs;

namespace StarPlex.Application.Features.Halls.Queries.GetHallById;

public class GetHallByIdQuery : IRequest<HallDto?>
{
    public Guid Id { get; set; }
}