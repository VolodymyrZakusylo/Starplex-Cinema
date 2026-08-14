using System.ComponentModel.DataAnnotations;

namespace StarPlex.Application.Common.Models;

public class StripeSettings
{
    [Required]
    public string SecretKey { get; set; } = string.Empty;
    [Required]
    public string PublishableKey { get; set; } = string.Empty;
    [Required]
    public string WebhookSecret { get; set; } = string.Empty;
}