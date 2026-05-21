using Microsoft.AspNetCore.Builder;
using TaskApp.Api.Middleware;

namespace TaskApp.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

}
