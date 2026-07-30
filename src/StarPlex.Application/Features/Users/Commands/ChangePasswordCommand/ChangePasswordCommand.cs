using MediatR;
using System;

namespace StarPlex.Application.Features.Users.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string OldPassword, string NewPassword) : IRequest<bool>;