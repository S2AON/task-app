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
                b =>
                {
                    b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                });
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
            configuration["JwtSettings:Secret"] ?? "your-super-secret-key-min-32-chars-long",
            configuration["JwtSettings:Issuer"] ?? "TaskApp"
        ));

        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        services.AddMemoryCache();

        return services;
    }
}
