using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using System;

namespace StarPlex.API;

public static class DependencyInjectionObservability
{
    public static IServiceCollection AddStarPlexObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var aiConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(aiConnectionString))
        {
            return services;
        }

        var managedIdentityClientId = configuration["Observability:ManagedIdentityClientId"];

        if (string.IsNullOrWhiteSpace(managedIdentityClientId) || !Guid.TryParse(managedIdentityClientId, out _))
        {
            throw new InvalidOperationException("Observability:ManagedIdentityClientId must be a valid GUID when APPLICATIONINSIGHTS_CONNECTION_STRING is present.");
        }

        var managedIdentityId = ManagedIdentityId.FromUserAssignedClientId(managedIdentityClientId);
        var credential = new ManagedIdentityCredential(managedIdentityId);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("StarPlex.API"))
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = aiConnectionString;
                options.Credential = credential;
            });

        return services;
    }
}
