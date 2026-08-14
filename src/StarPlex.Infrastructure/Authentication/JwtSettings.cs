using System.ComponentModel.DataAnnotations;

namespace StarPlex.Infrastructure.Authentication;

public class JwtSettings
{
    [Required]
    public string Secret { get; set; } = string.Empty;
    [Required]
    public string Issuer { get; set; } = string.Empty;
    [Required]
    public string Audience { get; set; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int ExpiryInMinutes { get; set; }
}