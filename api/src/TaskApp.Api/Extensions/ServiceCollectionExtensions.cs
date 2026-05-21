using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskApp.Api.Filters;
using TaskApp.Application.Interfaces;
using TaskApp.Infrastructure.Data;
using TaskApp.Infrastructure.Repositories;
using TaskApp.Infrastructure.Services;
using TaskFactory = TaskApp.Application.Factories.TaskFactory;

namespace TaskApp.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ValidationFilter>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<ITaskFactory, TaskFactory>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Register HttpContextAccessor for audit interceptor
        services.AddHttpContextAccessor();

        // Database Context with Audit Interceptor
        services.AddDbContext<ApplicationDbContext>((_, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton(_ => new JwtService(
            configuration["JWT_SECRET"] ?? "your-super-secret-key-min-32-chars-long",
            configuration["JWT_ISSUER"] ?? "TaskApp"
        ));

        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        services.AddMemoryCache();

        // Register BetterAuth JWKS HttpClient (following existing pattern like SIGERH-Client)
        services.AddHttpClient("BetterAuth-JWKS-Client", configureClient =>
        {
            var jwksUrl = configuration["Authentication:JwksUrl"];
            if (!string.IsNullOrWhiteSpace(jwksUrl))
            {
                var uri = new Uri(jwksUrl);
                configureClient.BaseAddress = new Uri($"{uri.Scheme}://{uri.Host}");
            }
        });

        // Register JWKS Service for fetching and caching signing keys
        // services.AddScoped<IJwksService, JwksService>();

        // Configure JWT Bearer Authentication with EdDSA support
        // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //     .AddJwtBearer(options =>
        //     {
        //         var validIssuer = configuration["Authentication:ValidIssuer"];
        //         var validAudience = configuration["Authentication:ValidAudience"];
        //         var clockSkewStr = configuration["Authentication:ClockSkew"];
        //
        //         var validateAudience =
        //             bool.TryParse(configuration["Authentication:ValidateAudience"], out var va) && va;
        //
        //         options.TokenValidationParameters = new TokenValidationParameters
        //         {
        //             ValidateIssuer = bool.TryParse(configuration["Authentication:ValidateIssuer"], out var vi) && vi,
        //             ValidateAudience = validateAudience,
        //             ValidateLifetime =
        //                 bool.TryParse(configuration["Authentication:ValidateLifetime"], out var vl) && vl,
        //             ValidateIssuerSigningKey = false, // We'll validate EdDSA signatures manually in middleware
        //             ValidIssuer = validIssuer,
        //             ValidAudience = validAudience,
        //             ClockSkew = TimeSpan.TryParse(clockSkewStr, out var cs) ? cs : TimeSpan.FromMinutes(5),
        //             // Microsoft.IdentityModel 8.x changed audience validation behavior for JsonWebToken.
        //             // Using an explicit validator avoids the IDX10214 regression with array-typed aud claims.
        //             AudienceValidator = (audiences, _, parameters) =>
        //             {
        //                 if (!validateAudience) return true;
        //                 var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //                 if (!string.IsNullOrEmpty(parameters.ValidAudience)) valid.Add(parameters.ValidAudience);
        //                 if (parameters.ValidAudiences != null)
        //                     foreach (var a in parameters.ValidAudiences)
        //                         valid.Add(a);
        //                 return audiences?.Any(a => valid.Contains(a)) ?? false;
        //             },
        //             // Custom signature validator that skips EdDSA (already validated in middleware)
        //             SignatureValidator = (token, parameters) =>
        //             {
        //                 // For EdDSA tokens, skip signature validation here (done in middleware)
        //                 var handler = new JsonWebTokenHandler();
        //                 try
        //                 {
        //                     var jwt = handler.ReadJsonWebToken(token);
        //                     if (jwt.Alg == "EdDSA")
        //                         // EdDSA signature already validated in EdDsaValidationMiddleware
        //                         return jwt;
        //                 }
        //                 catch
        //                 {
        //                 }
        //
        //                 // For non-EdDSA, return a dummy token (signature will be validated by JWT Bearer)
        //                 return new JsonWebToken(token);
        //             },
        //             // Custom key resolver for EdDSA support
        //             IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
        //             {
        //                 try
        //                 {
        //                     // Build a temporary service provider to get IJwksService
        //                     var serviceProvider = services.BuildServiceProvider();
        //                     var jwksService = serviceProvider.GetRequiredService<IJwksService>();
        //                     var keys = jwksService.GetSigningKeysAsync().GetAwaiter().GetResult();
        //
        //                     if (!string.IsNullOrWhiteSpace(kid))
        //                     {
        //                         var filtered = keys.Where(k => k.KeyId == kid).ToList();
        //                         return filtered.Count > 0 ? filtered : keys.ToList();
        //                     }
        //
        //                     return keys.ToList();
        //                 }
        //                 catch (Exception ex)
        //                 {
        //                     var logger = services.BuildServiceProvider()
        //                         .GetRequiredService<ILogger<JwtBearerEvents>>();
        //                     logger.LogError(ex, "Failed to resolve signing keys from JWKS");
        //                     return new List<SecurityKey>();
        //                 }
        //             }
        //         };
        //
        //         // Add event handlers for logging authentication results
        //         options.Events = new JwtBearerEvents
        //         {
        //             OnAuthenticationFailed = context =>
        //             {
        //                 var logger = context.HttpContext.RequestServices
        //                     .GetRequiredService<ILogger<JwtBearerEvents>>();
        //                 try
        //                 {
        //                     var raw = context.HttpContext.Request.Headers.Authorization
        //                         .ToString().Replace("Bearer ", "");
        //                     if (!string.IsNullOrEmpty(raw))
        //                     {
        //                         var handler = new JsonWebTokenHandler();
        //                         var jwt = handler.ReadJsonWebToken(raw);
        //                         logger.LogWarning(
        //                             "JWT failed — token aud: [{Audiences}], configured ValidAudience: [{ValidAudience}]",
        //                             string.Join(", ", jwt.Audiences), validAudience);
        //                     }
        //                 }
        //                 catch
        //                 {
        //                     /* best-effort diagnostic */
        //                 }
        //
        //                 logger.LogWarning(
        //                     context.Exception,
        //                     "JWT authentication failed for request to {Path}. Exception: {ExceptionMessage}",
        //                     context.HttpContext.Request.Path,
        //                     context.Exception?.Message);
        //                 return Task.CompletedTask;
        //             },
        //
        //             OnTokenValidated = context =>
        //             {
        //                 var logger = context.HttpContext.RequestServices
        //                     .GetRequiredService<ILogger<JwtBearerEvents>>();
        //                 var userIdentifier = context.Principal?.FindFirst("sub")?.Value ?? "unknown";
        //                 logger.LogInformation(
        //                     "JWT token validated successfully for user {UserId}",
        //                     userIdentifier);
        //                 return Task.CompletedTask;
        //             },
        //
        //             OnChallenge = context =>
        //             {
        //                 var logger = context.HttpContext.RequestServices
        //                     .GetRequiredService<ILogger<JwtBearerEvents>>();
        //                 logger.LogWarning(
        //                     "JWT challenge issued for request to {Path}. Reason: {AuthenticateFailure}",
        //                     context.HttpContext.Request.Path,
        //                     context.AuthenticateFailure?.Message);
        //                 return Task.CompletedTask;
        //             }
        //         };
        //     });

        return services;
    }
}
