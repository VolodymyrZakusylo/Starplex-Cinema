using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarPlex.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace StarPlex.API.IntegrationTests.Infrastructure;

public class DependencyInjectionDataProtectionTests
{
    private IServiceCollection _services;
    private string _contentRootPath;

    public DependencyInjectionDataProtectionTests()
    {
        _services = new ServiceCollection();
        _contentRootPath = Directory.GetCurrentDirectory();
    }

    private IConfiguration CreateConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void AddInfrastructureDataProtection_DefaultProvider_IsFileSystem()
    {
        var config = CreateConfig(new Dictionary<string, string?>());

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddInfrastructureDataProtection_UnknownProvider_ThrowsInvalidOperationException()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:Provider", "UnknownProvider" }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Invalid DataProtection provider: UnknownProvider*");
    }

    [Theory]
    [InlineData(null, "https://kv.vault.azure.net/keys/k1", "00000000-0000-0000-0000-000000000000")]
    [InlineData("", "https://kv.vault.azure.net/keys/k1", "00000000-0000-0000-0000-000000000000")]
    public void AddInfrastructureDataProtection_AzureProvider_MissingBlobUri_Throws(string? blobUri, string keyId, string clientId)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:Provider", "Azure" },
            { "DataProtection:BlobUri", blobUri },
            { "DataProtection:KeyIdentifier", keyId },
            { "DataProtection:ManagedIdentityClientId", clientId }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("DataProtection BlobUri is required when Provider is Azure.");
    }

    [Theory]
    [InlineData("http://insecure.blob.core.windows.net/dp/keys.xml", "DataProtection BlobUri must be a valid absolute HTTPS URI.")]
    [InlineData("not-a-uri", "DataProtection BlobUri must be a valid absolute HTTPS URI.")]
    [InlineData("https://st.blob.core.windows.net/dp/keys.xml?sasToken=123", "DataProtection BlobUri must not contain a query string or fragment.")]
    [InlineData("https://st.blob.core.windows.net/dp/keys.xml#fragment", "DataProtection BlobUri must not contain a query string or fragment.")]
    public void AddInfrastructureDataProtection_AzureProvider_MalformedBlobUri_Throws(string blobUri, string expectedMessage)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:Provider", "Azure" },
            { "DataProtection:BlobUri", blobUri },
            { "DataProtection:KeyIdentifier", "https://kv.vault.azure.net/keys/k1" },
            { "DataProtection:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage(expectedMessage);
    }

    [Theory]
    [InlineData(null, "https://st.blob.core.windows.net/dp/keys.xml", "00000000-0000-0000-0000-000000000000")]
    [InlineData("", "https://st.blob.core.windows.net/dp/keys.xml", "00000000-0000-0000-0000-000000000000")]
    public void AddInfrastructureDataProtection_AzureProvider_MissingKeyIdentifier_Throws(string? keyId, string blobUri, string clientId)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:Provider", "Azure" },
            { "DataProtection:BlobUri", blobUri },
            { "DataProtection:KeyIdentifier", keyId },
            { "DataProtection:ManagedIdentityClientId", clientId }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("DataProtection KeyIdentifier is required when Provider is Azure.");
    }

    [Theory]
    [InlineData("http://insecure.vault.azure.net/keys/k1", "DataProtection KeyIdentifier must be a valid absolute HTTPS URI.")]
    [InlineData("not-a-uri", "DataProtection KeyIdentifier must be a valid absolute HTTPS URI.")]
    [InlineData("https://kv.vault.azure.net/keys/k1?api-version=1.0", "DataProtection KeyIdentifier must not contain a query string or fragment.")]
    [InlineData("https://kv.vault.azure.net/keys/k1#frag", "DataProtection KeyIdentifier must not contain a query string or fragment.")]
    [InlineData("https://kv.vault.azure.net/keys/k1/version123", "DataProtection KeyIdentifier must be a versionless Key Vault key URI.")]
    [InlineData("https://kv.vault.azure.net/secrets/k1", "DataProtection KeyIdentifier must be a versionless Key Vault key URI.")]
    public void AddInfrastructureDataProtection_AzureProvider_MalformedKeyIdentifier_Throws(string keyId, string expectedMessage)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:Provider", "Azure" },
            { "DataProtection:BlobUri", "https://st.blob.core.windows.net/dp/keys.xml" },
            { "DataProtection:KeyIdentifier", keyId },
            { "DataProtection:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage(expectedMessage);
    }

    [Theory]
    [InlineData("https://kv.vault.azure.net/keys/k1")]
    [InlineData("https://kv.vault.azure.net/keys/k1/")]
    public void AddInfrastructureDataProtection_AzureProvider_ValidVersionlessKeyIdentifier_DoesNotThrowValidationErrors(string keyId)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:Provider", "Azure" },
            { "DataProtection:BlobUri", "https://st.blob.core.windows.net/dp/keys.xml" },
            { "DataProtection:KeyIdentifier", keyId },
            { "DataProtection:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().NotThrow<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void AddInfrastructureDataProtection_AzureProvider_InvalidClientId_Throws(string? clientId)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:Provider", "Azure" },
            { "DataProtection:BlobUri", "https://st.blob.core.windows.net/dp/keys.xml" },
            { "DataProtection:KeyIdentifier", "https://kv.vault.azure.net/keys/k1" },
            { "DataProtection:ManagedIdentityClientId", clientId }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("DataProtection ManagedIdentityClientId must be a valid GUID when Provider is Azure.");
    }

    [Theory]
    [InlineData("https://account.blob.core.windows.net", "DataProtection BlobUri must point to a concrete blob, not merely a storage account or container root.")]
    [InlineData("https://account.blob.core.windows.net/", "DataProtection BlobUri must point to a concrete blob, not merely a storage account or container root.")]
    [InlineData("https://account.blob.core.windows.net/dataprotection", "DataProtection BlobUri must point to a concrete blob, not merely a storage account or container root.")]
    [InlineData("https://account.blob.core.windows.net/dataprotection/", "DataProtection BlobUri must point to a concrete blob, not merely a storage account or container root.")]
    public void AddInfrastructureDataProtection_AzureProvider_NonConcreteBlobUri_Throws(string blobUri, string expectedMessage)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:Provider", "Azure" },
            { "DataProtection:BlobUri", blobUri },
            { "DataProtection:KeyIdentifier", "https://kv.vault.azure.net/keys/k1" },
            { "DataProtection:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage(expectedMessage);
    }

    [Theory]
    [InlineData("https://account.blob.core.windows.net/dataprotection/keys.xml")]
    [InlineData("https://account.blob.core.windows.net/dataprotection/folder/keys.xml")]
    public void AddInfrastructureDataProtection_AzureProvider_ConcreteBlobUri_DoesNotThrowValidationErrors(string blobUri)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:Provider", "Azure" },
            { "DataProtection:BlobUri", blobUri },
            { "DataProtection:KeyIdentifier", "https://kv.vault.azure.net/keys/k1" },
            { "DataProtection:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().NotThrow<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddInfrastructureDataProtection_EmptyApplicationName_Throws(string applicationName)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "DataProtection:ApplicationName", applicationName }
        });

        Action act = () => _services.AddInfrastructureDataProtection(config, _contentRootPath);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("DataProtection ApplicationName must not be empty or whitespace.");
    }
}
