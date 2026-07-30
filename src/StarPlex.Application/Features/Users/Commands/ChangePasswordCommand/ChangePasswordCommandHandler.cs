using MediatR;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return false;
        }

        return await _identityService.ChangePasswordAsync(request.UserId, request.OldPassword, request.NewPassword);
    }
}