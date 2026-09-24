using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarPlex.API;
using System;
using System.Collections.Generic;
using Xunit;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace StarPlex.API.IntegrationTests;

public class DependencyInjectionObservabilityTests
{
    [Fact]
    public void AddStarPlexObservability_NoConnectionString_DoesNotThrowAndDoesNotRegisterOpenTelemetry()
    {
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        var configuration = configBuilder.Build();

        var exception = Record.Exception(() => services.AddStarPlexObservability(configuration));

        Assert.Null(exception);
        
        var serviceProvider = services.BuildServiceProvider();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.Null(tracerProvider);
    }

    [Fact]
    public void AddStarPlexObservability_ConnectionStringPresent_MissingClientId_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "APPLICATIONINSIGHTS_CONNECTION_STRING", "InstrumentationKey=00000000-0000-0000-0000-000000000000" }
        });
        var configuration = configBuilder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddStarPlexObservability(configuration));

        Assert.Contains("Observability:ManagedIdentityClientId must be a valid GUID", exception.Message);
    }

    [Fact]
    public void AddStarPlexObservability_ConnectionStringPresent_InvalidClientId_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "APPLICATIONINSIGHTS_CONNECTION_STRING", "InstrumentationKey=00000000-0000-0000-0000-000000000000" },
            { "Observability:ManagedIdentityClientId", "not-a-guid" }
        });
        var configuration = configBuilder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddStarPlexObservability(configuration));

        Assert.Contains("Observability:ManagedIdentityClientId must be a valid GUID", exception.Message);
    }

    [Fact]
    public void AddStarPlexObservability_ConnectionStringPresent_ValidClientId_RegistersOpenTelemetry()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "APPLICATIONINSIGHTS_CONNECTION_STRING", "InstrumentationKey=00000000-0000-0000-0000-000000000000" },
            { "Observability:ManagedIdentityClientId", Guid.NewGuid().ToString() }
        });
        var configuration = configBuilder.Build();

        var exception = Record.Exception(() => services.AddStarPlexObservability(configuration));

        Assert.Null(exception);
        
        Assert.Contains(services, d => d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("OpenTelemetry"));
    }
}
