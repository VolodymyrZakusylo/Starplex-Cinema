namespace StarPlex.Application.Common.Interfaces;

public interface IUserService
{
    Task<(string Email, string FullName)?> GetUserContactInfoAsync(Guid userId, CancellationToken ct);
}