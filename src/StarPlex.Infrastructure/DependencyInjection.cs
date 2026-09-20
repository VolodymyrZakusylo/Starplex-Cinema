using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;
using StarPlex.Infrastructure.Authentication;
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
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

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
}
