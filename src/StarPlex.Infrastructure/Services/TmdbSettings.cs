using System.ComponentModel.DataAnnotations;

namespace StarPlex.Infrastructure.Services;

public class TmdbSettings
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}
