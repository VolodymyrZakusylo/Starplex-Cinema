namespace StarPlex.Application.Common.Models;

public class UserStaffDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
    public Guid? CinemaId { get; set; }
    public string? CinemaName { get; set; }
}