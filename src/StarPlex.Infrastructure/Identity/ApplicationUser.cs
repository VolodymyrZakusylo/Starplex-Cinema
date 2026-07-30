using Microsoft.AspNetCore.Identity;
using StarPlex.Domain.Entities;

namespace StarPlex.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CinemaId { get; set; }

    public ApplicationUser()
    {

    }

    public ApplicationUser(string firstName, string lastName, DateTime dateOfBirth, Guid? cinemaId = null)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Utc);
        CreatedAt = DateTime.UtcNow;
        CinemaId = cinemaId;

    }
}