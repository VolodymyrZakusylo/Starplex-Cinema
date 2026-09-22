using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarPlex.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace StarPlex.API.IntegrationTests.Infrastructure;

public class DependencyInjectionEmailTests
{
    private IServiceCollection _services;
    private string _contentRootPath;

    public DependencyInjectionEmailTests()
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
    public void AddInfrastructureServices_UnknownProvider_ThrowsInvalidOperationException()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:Provider", "UnknownProvider" },
            { "EmailSettings:FromEmail", "test@test.com" },
            { "EmailSettings:FromName", "Test" }
        });

        Action act = () => _services.AddInfrastructureEmail(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Invalid Email provider: UnknownProvider*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddInfrastructureServices_AzureProvider_MissingEndpoint_Throws(string? endpoint)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:Provider", "Azure" },
            { "EmailSettings:FromEmail", "test@test.com" },
            { "EmailSettings:FromName", "Test" },
            { "AzureEmail:Endpoint", endpoint },
            { "AzureEmail:SenderAddress", "test@test.com" },
            { "AzureEmail:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        Action act = () => _services.AddInfrastructureEmail(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("AzureEmail Endpoint must be a valid absolute HTTPS URI.");
    }

    [Theory]
    [InlineData("http://insecure.endpoint.net")]
    [InlineData("not-a-uri")]
    [InlineData("https://acs.com?query=1")]
    [InlineData("https://acs.com#frag")]
    public void AddInfrastructureServices_AzureProvider_MalformedEndpoint_Throws(string endpoint)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:Provider", "Azure" },
            { "EmailSettings:FromEmail", "test@test.com" },
            { "EmailSettings:FromName", "Test" },
            { "AzureEmail:Endpoint", endpoint },
            { "AzureEmail:SenderAddress", "test@test.com" },
            { "AzureEmail:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        Action act = () => _services.AddInfrastructureEmail(config);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void AddInfrastructureServices_AzureProvider_InvalidSenderAddress_Throws(string? sender)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:Provider", "Azure" },
            { "EmailSettings:FromEmail", "test@test.com" },
            { "EmailSettings:FromName", "Test" },
            { "AzureEmail:Endpoint", "https://acs.com" },
            { "AzureEmail:SenderAddress", sender },
            { "AzureEmail:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        Action act = () => _services.AddInfrastructureEmail(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("AzureEmail SenderAddress must be a valid email address.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void AddInfrastructureServices_AzureProvider_InvalidClientId_Throws(string? clientId)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:Provider", "Azure" },
            { "EmailSettings:FromEmail", "test@test.com" },
            { "EmailSettings:FromName", "Test" },
            { "AzureEmail:Endpoint", "https://acs.com" },
            { "AzureEmail:SenderAddress", "test@test.com" },
            { "AzureEmail:ManagedIdentityClientId", clientId }
        });

        Action act = () => _services.AddInfrastructureEmail(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("AzureEmail ManagedIdentityClientId must be a valid GUID.");
    }

    [Fact]
    public void AddInfrastructureServices_DefaultProvider_IsSmtp()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:SmtpHost", "localhost" },
            { "EmailSettings:SmtpPort", "1025" },
            { "EmailSettings:FromEmail", "test@test.com" },
            { "EmailSettings:FromName", "Test" }
        });

        Action act = () => _services.AddInfrastructureEmail(config);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddInfrastructureServices_SmtpProvider_MissingHost_Throws(string? host)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:SmtpHost", host },
            { "EmailSettings:SmtpPort", "1025" },
            { "EmailSettings:FromEmail", "test@test.com" },
            { "EmailSettings:FromName", "Test" }
        });

        Action act = () => _services.AddInfrastructureEmail(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("EmailSettings SmtpHost is required when Provider is Smtp.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void AddInfrastructureServices_SmtpProvider_InvalidFromEmail_Throws(string? email)
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:SmtpHost", "localhost" },
            { "EmailSettings:SmtpPort", "1025" },
            { "EmailSettings:FromEmail", email },
            { "EmailSettings:FromName", "Test" }
        });

        Action act = () => _services.AddInfrastructureEmail(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("EmailSettings FromEmail must be a valid email address when Provider is Smtp.");
    }

    [Fact]
    public void AddInfrastructureServices_AzureProvider_ValidConfig_SucceedsWithoutSmtpSettings()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:Provider", "Azure" },
            { "EmailSettings:FromName", "Test" },
            { "AzureEmail:Endpoint", "https://acs.com" },
            { "AzureEmail:SenderAddress", "test@test.com" },
            { "AzureEmail:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        Action act = () => _services.AddInfrastructureEmail(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddInfrastructureServices_AzureProvider_ResolvesAsAzureEmailService()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:Provider", "Azure" },
            { "EmailSettings:FromName", "Test" },
            { "AzureEmail:Endpoint", "https://acs.com" },
            { "AzureEmail:SenderAddress", "test@test.com" },
            { "AzureEmail:ManagedIdentityClientId", "00000000-0000-0000-0000-000000000000" }
        });

        _services.AddLogging(); // required for ILogger
        _services.AddInfrastructureEmail(config);

        var provider = _services.BuildServiceProvider();
        var service = provider.GetRequiredService<StarPlex.Application.Common.Interfaces.IEmailService>();

        service.Should().BeOfType<StarPlex.Infrastructure.Services.AzureEmailService>();
    }

    [Fact]
    public void AddInfrastructureServices_SmtpProvider_ResolvesAsEmailService()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            { "EmailSettings:Provider", "Smtp" },
            { "EmailSettings:SmtpHost", "localhost" },
            { "EmailSettings:SmtpPort", "1025" },
            { "EmailSettings:FromEmail", "test@test.com" },
            { "EmailSettings:FromName", "Test" }
        });

        _services.AddLogging();
        _services.AddInfrastructureEmail(config);

        var provider = _services.BuildServiceProvider();
        var service = provider.GetRequiredService<StarPlex.Application.Common.Interfaces.IEmailService>();

        service.Should().BeOfType<StarPlex.Infrastructure.Services.EmailService>();
    }
}
