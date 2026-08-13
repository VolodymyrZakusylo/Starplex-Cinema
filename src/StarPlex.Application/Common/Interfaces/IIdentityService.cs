using StarPlex.Application.Common.Models;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<AuthResponse?> RegisterAsync(string email, string password, string firstName, string lastName, DateTime dateOfBirth, CancellationToken cancellationToken = default);
    Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<bool> UpdateProfileAsync(Guid userId, string firstName, string lastName);
    Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    Task<bool> DeleteAccountAsync(Guid userId);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email);
    Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<(List<UserStaffDto> Users, int TotalCount)> SearchUsersAsync(string searchTerm, UserRole? roleFilter, Guid? cinemaIdFilter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> UpdateUserRoleAndCinemaAsync(Guid userId, UserRole newRole, Guid? cinemaId);
}