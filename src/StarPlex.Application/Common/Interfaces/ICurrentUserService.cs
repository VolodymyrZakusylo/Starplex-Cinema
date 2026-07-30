namespace StarPlex.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Role { get; }
    Guid? CinemaId { get; }
    bool IsSuperAdmin { get; }
}