using MediatR;

namespace StarPlex.Application.Features.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, string FirstName, string LastName) : IRequest<bool>;