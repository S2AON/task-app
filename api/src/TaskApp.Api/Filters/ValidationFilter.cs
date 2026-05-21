using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskApp.Api.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            var errorResponse = new
            {
                Success = false,
                Message = "Validation errors occurred",
                Errors = errors
            };

            context.Result = new BadRequestObjectResult(errorResponse);
            return;
        }

        if (context.ActionArguments.Count == 0)
        {
            var errorResponse = new
            {
                Success = false,
                Message = "Object is null",
                Errors = new List<string> { "Object sent from client is null." }
            };
            context.Result = new BadRequestObjectResult(errorResponse);
            return;
        }

        await next();
    }
}
