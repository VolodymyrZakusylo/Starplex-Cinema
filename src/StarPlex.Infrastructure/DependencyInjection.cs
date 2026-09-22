using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;
using StarPlex.Infrastructure.Authentication;
using StarPlex.Infrastructure.Configuration;
using StarPlex.Infrastructure.Identity;
using StarPlex.Infrastructure.Persistence;
using StarPlex.Infrastructure.Services;
using System.Text;

namespace StarPlex.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, builder =>
                builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        var jwtSection = configuration.GetSection("JwtSettings");
        services.AddOptions<JwtSettings>()
            .Bind(jwtSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        var jwtSettings = jwtSection.Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");

        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.HttpContext.Request.Path.StartsWithSegments("/hub/seats"))
                    {
                        var accessToken = context.HttpContext.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddOptions<TmdbSettings>()
            .Bind(configuration.GetSection("TmdbSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ITmdbService, TmdbService>((provider, client) =>
        {
            var tmdbSettings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TmdbSettings>>().Value;
            client.BaseAddress = new Uri(tmdbSettings.BaseUrl);
        });

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ISeatLockService, PostgresSeatLockService>();
        var storageSettingsSection = configuration.GetSection(StorageSettings.SectionName);
        services.Configure<StorageSettings>(storageSettingsSection);
        var storageSettings = storageSettingsSection.Get<StorageSettings>() ?? new StorageSettings();

        if (storageSettings.Provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            var azureBlobSection = configuration.GetSection(AzureBlobStorageSettings.SectionName);
            services.Configure<AzureBlobStorageSettings>(azureBlobSection);
            var azureSettings = azureBlobSection.Get<AzureBlobStorageSettings>();

            if (azureSettings == null || string.IsNullOrWhiteSpace(azureSettings.ServiceUri) || string.IsNullOrWhiteSpace(azureSettings.ContainerName))
            {
                throw new InvalidOperationException("AzureBlobStorage configuration is invalid or missing.");
            }

            if (!Uri.TryCreate(azureSettings.ServiceUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("AzureBlobStorage ServiceUri must be a valid absolute HTTPS URI.");
            }

            services.AddSingleton(provider =>
            {
                var credentialOptions = new Azure.Identity.DefaultAzureCredentialOptions();
                if (!string.IsNullOrWhiteSpace(azureSettings.ManagedIdentityClientId))
                {
                    credentialOptions.ManagedIdentityClientId = azureSettings.ManagedIdentityClientId;
                }

                return new Azure.Storage.Blobs.BlobServiceClient(
                    uri,
                    new Azure.Identity.DefaultAzureCredential(credentialOptions));
            });

            services.AddScoped<IFileStorageService, AzureBlobFileStorageService>();
        }
        else if (storageSettings.Provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }
        else
        {
            throw new InvalidOperationException($"Invalid file storage provider: {storageSettings.Provider}. Supported providers: Local, AzureBlob.");
        }

        services.AddHostedService<ExpiredLocksCleanupService>();
        services.AddHostedService<AuditLogCleanupService>();

        services.AddScoped<ISeatHubService, SeatHubService>();

        services.AddOptions<StripeSettings>()
            .Bind(configuration.GetSection("StripeSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IPaymentService, StripePaymentService>();

        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<ITicketService, TicketService>();

        services.AddScoped<IUserService, UserService>();
        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection("EmailSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureDataProtection(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var section = configuration.GetSection(DataProtectionSettings.SectionName);
        services.Configure<DataProtectionSettings>(section);
        var settings = section.Get<DataProtectionSettings>() ?? new DataProtectionSettings();

        if (string.IsNullOrWhiteSpace(settings.ApplicationName))
        {
            throw new InvalidOperationException("DataProtection ApplicationName must not be empty or whitespace.");
        }

        var dpBuilder = Microsoft.Extensions.DependencyInjection.DataProtectionServiceCollectionExtensions.AddDataProtection(services)
            .SetApplicationName(settings.ApplicationName);

        if (settings.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(settings.BlobUri))
                throw new InvalidOperationException("DataProtection BlobUri is required when Provider is Azure.");

            if (!Uri.TryCreate(settings.BlobUri, UriKind.Absolute, out var blobUri) || blobUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("DataProtection BlobUri must be a valid absolute HTTPS URI.");
            if (!string.IsNullOrEmpty(blobUri.Query) || !string.IsNullOrEmpty(blobUri.Fragment))
                throw new InvalidOperationException("DataProtection BlobUri must not contain a query string or fragment.");
            if (blobUri.Segments.Length < 3 || blobUri.Segments[^1].EndsWith('/'))
                throw new InvalidOperationException("DataProtection BlobUri must point to a concrete blob, not merely a storage account or container root.");

            if (string.IsNullOrWhiteSpace(settings.KeyIdentifier))
                throw new InvalidOperationException("DataProtection KeyIdentifier is required when Provider is Azure.");

            if (!Uri.TryCreate(settings.KeyIdentifier, UriKind.Absolute, out var keyUri) || keyUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("DataProtection KeyIdentifier must be a valid absolute HTTPS URI.");
            if (!string.IsNullOrEmpty(keyUri.Query) || !string.IsNullOrEmpty(keyUri.Fragment))
                throw new InvalidOperationException("DataProtection KeyIdentifier must not contain a query string or fragment.");
            if (keyUri.Segments.Length != 3 || !keyUri.Segments[1].TrimEnd('/').Equals("keys", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("DataProtection KeyIdentifier must be a versionless Key Vault key URI.");

            if (string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId) || !Guid.TryParse(settings.ManagedIdentityClientId, out _))
                throw new InvalidOperationException("DataProtection ManagedIdentityClientId must be a valid GUID when Provider is Azure.");

            var managedIdentityId = Azure.Identity.ManagedIdentityId.FromUserAssignedClientId(settings.ManagedIdentityClientId);
            var credential = new Azure.Identity.ManagedIdentityCredential(managedIdentityId);

            dpBuilder.PersistKeysToAzureBlobStorage(blobUri, credential)
                     .ProtectKeysWithAzureKeyVault(keyUri, credential);
        }
        else if (settings.Provider.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
        {
            dpBuilder.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(contentRootPath, "dp_keys")));
        }
        else
        {
            throw new InvalidOperationException($"Invalid DataProtection provider: {settings.Provider}. Supported providers: FileSystem, Azure.");
        }

        return services;
    }
}
