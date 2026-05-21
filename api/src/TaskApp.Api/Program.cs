using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskApp.Infrastructure.Data;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using TaskApp.Api.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(config => { config.AddEnvironmentVariables(); })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddSingleton<IOpenApiConfigurationOptions>(_ => new OpenApiConfigurationOptions
        {
            Info = new OpenApiInfo
            {
                Version = "v1",
                Title = "Task App API",
                Description = "RESTful API for managing tasks with JWT authentication",
                Contact = new OpenApiContact
                {
                    Name = "Task App Support",
                    Email = "support@taskapp.com"
                }
            },
            Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
            OpenApiVersion = OpenApiVersionType.V3,
            IncludeRequestingHostName = true,
            ForceHttps = false,
            ForceHttp = false
        });

        services.AddApplicationServices();
        services.AddInfrastructureServices(context.Configuration);
        services.AddAuthenticationServices(context.Configuration);
        services.AddCorsPolicy();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed. Application will continue running.");
    }
}

await host.RunAsync();
