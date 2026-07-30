using MediatR;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, bool>
{
    private readonly IIdentityService _identityService;

    public UpdateProfileCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<bool> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return false;
        }

        return await _identityService.UpdateProfileAsync(request.UserId, request.FirstName, request.LastName);
    }
}