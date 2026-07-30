using MediatR;

namespace StarPlex.Application.Features.Sessions.Commands.CancelSession;

public class CancelSessionCommand : IRequest
{
    public Guid Id { get; set; }
}