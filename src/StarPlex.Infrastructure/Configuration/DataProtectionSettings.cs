namespace StarPlex.Infrastructure.Configuration;

public class DataProtectionSettings
{
    public const string SectionName = "DataProtection";

    public string Provider { get; set; } = "FileSystem";
    public string ApplicationName { get; set; } = "StarPlex";
    
    public string? BlobUri { get; set; }
    public string? KeyIdentifier { get; set; }
    public string? ManagedIdentityClientId { get; set; }
}
