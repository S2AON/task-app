using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TaskApp.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var response = context.Response;

        var errorResponse = new ErrorResponse
        {
            Success = false
        };

        switch (exception)
        {
            case DbUpdateException dbUpdateEx when dbUpdateEx.InnerException is SqlException sqlEx:
                HandleSqlException(sqlEx, response, errorResponse);
                logger.LogError(sqlEx, "Database error occurred: {Message}", sqlEx.Message);
                break;

            case DbUpdateException dbUpdateEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Error al actualizar la base de datos. " +
                                        (dbUpdateEx.InnerException?.Message ?? dbUpdateEx.Message);
                logger.LogError(dbUpdateEx, "Database update error occurred: {Message}", dbUpdateEx.Message);
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = exception.Message;
                logger.LogWarning(exception, "Resource not found: {Message}", exception.Message);
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = "Unauthorized access";
                logger.LogWarning(exception, "Unauthorized access attempt");
                break;

            case ArgumentException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Uno o mas parametros son incorrectos";
                logger.LogWarning(exception, "Uno o mas parametros son incorrectos");
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "Internal server error. Please try again later.";
                logger.LogError(exception, "Unexpected error occurred: {Message}", exception.Message);
                break;
        }

        var result = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(result);
    }

    private void HandleSqlException(SqlException sqlException, HttpResponse response, ErrorResponse errorResponse)
    {
        switch (sqlException.Number)
        {
            case 547: // Foreign key constraint
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message =
                    "Error de integridad referencial. Una o más claves foráneas no son válidas. Verifique que los registros relacionados existan.";
                break;

            case 2627: // Unique constraint error
            case 2601: // Duplicated key row error
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Violación de restricción única. Ya existe un registro con estos valores.";
                break;

            case 515: // Cannot insert null
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Error de datos requeridos. Uno o más campos requeridos no tienen valor.";
                break;

            case 8152: // String or binary data would be truncated
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message =
                    "Error de longitud de datos. Uno o más campos exceden la longitud máxima permitida.";
                break;

            case -1: // Timeout
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Message = "Tiempo de espera agotado. La operación tardó demasiado tiempo en completarse.";
                break;

            case 1205: // Deadlock
                response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Message =
                    "Conflicto de recursos. Se detectó un conflicto al intentar acceder a los recursos. Por favor, intente nuevamente.";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = $"Error de base de datos: {sqlException.Message}";
                break;
        }
    }

    private class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
