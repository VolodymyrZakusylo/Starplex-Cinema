namespace StarPlex.Infrastructure.Configuration;

public class AzureEmailSettings
{
    public const string SectionName = "AzureEmail";

    public string? Endpoint { get; set; }
    public string? SenderAddress { get; set; }
    public string? ManagedIdentityClientId { get; set; }
}
