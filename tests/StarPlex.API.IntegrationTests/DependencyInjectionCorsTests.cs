using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StarPlex.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StarPlex.API.IntegrationTests;

public class DependencyInjectionCorsTests
{
    [Fact]
    public void AddStarPlexCors_ConfiguredLocalhostOrigin_RegistersSuccessfully()
    {
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Cors:AllowedOrigins:0", "http://localhost:5173" }
        });
        var configuration = configBuilder.Build();

        services.AddStarPlexCors(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var corsOptions = serviceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        
        var policy = corsOptions.GetPolicy("SignalRPolicy");
        Assert.NotNull(policy);
        Assert.Contains("http://localhost:5173", policy.Origins);
        Assert.True(policy.AllowAnyHeader);
        Assert.True(policy.AllowAnyMethod);
        Assert.True(policy.SupportsCredentials);
    }

    [Fact]
    public void AddStarPlexCors_MultipleConfiguredOrigins_RegistersSuccessfully()
    {
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Cors:AllowedOrigins:0", "http://localhost:5173" },
            { "Cors:AllowedOrigins:1", "https://starplex-web-dev.azurecontainerapps.io" }
        });
        var configuration = configBuilder.Build();

        services.AddStarPlexCors(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var corsOptions = serviceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        
        var policy = corsOptions.GetPolicy("SignalRPolicy");
        Assert.NotNull(policy);
        Assert.Contains("http://localhost:5173", policy.Origins);
        Assert.Contains("https://starplex-web-dev.azurecontainerapps.io", policy.Origins);
    }

    [Fact]
    public void AddStarPlexCors_MissingOrigins_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        var configuration = configBuilder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddStarPlexCors(configuration));

        Assert.Contains("CORS AllowedOrigins are not configured or empty.", exception.Message);
    }
}
