using System.ComponentModel.DataAnnotations;

namespace StarPlex.Infrastructure.Authentication;

public class EmailSettings
{
    [Required]
    public string SmtpHost { get; set; } = string.Empty;
    [Range(1, 65535)]
    public int SmtpPort { get; set; }
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string FromEmail { get; set; } = string.Empty;
    [Required]
    public string FromName { get; set; } = string.Empty;
}