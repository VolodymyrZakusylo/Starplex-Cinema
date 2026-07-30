using MediatR;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Users.Commands.DeleteAccount;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, bool>
{
    private readonly IIdentityService _identityService;

    public DeleteAccountCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.DeleteAccountAsync(request.UserId);
    }
}