namespace StarPlex.Infrastructure.Configuration;

public class StorageSettings
{
    public const string SectionName = "FileStorage";
    
    public string Provider { get; set; } = "Local";
}

public class AzureBlobStorageSettings
{
    public const string SectionName = "AzureBlobStorage";
    
    public string ServiceUri { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "media";
    public string? ManagedIdentityClientId { get; set; }
}
