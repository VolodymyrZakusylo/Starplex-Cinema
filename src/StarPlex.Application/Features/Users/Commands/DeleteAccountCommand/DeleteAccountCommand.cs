using MediatR;

namespace StarPlex.Application.Features.Users.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid UserId) : IRequest<bool>;