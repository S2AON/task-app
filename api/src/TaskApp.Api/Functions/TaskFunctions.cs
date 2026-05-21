using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using TaskApp.Api.Middleware;
using TaskApp.Api.OpenApi;
using TaskApp.Application.Dtos;
using TaskApp.Application.Interfaces;
using TaskApp.Infrastructure.Services;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Api.Functions;

public class TaskFunctions(
    ILogger<TaskFunctions> logger,
    ITaskService taskService,
    JwtService jwtService)
{
    [Function("GetTasks")]
    [OpenApiOperation(operationId: "GetTasks", tags: ["Tasks"], Summary = "Get all tasks with optional filters")]
    [JwtAuthorization]
    [OpenApiParameter(name: "search", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
    [OpenApiParameter(name: "status", In = ParameterLocation.Query, Required = false, Type = typeof(int))]
    [OpenApiParameter(name: "assignedTo", In = ParameterLocation.Query, Required = false, Type = typeof(Guid))]
    [OpenApiParameter(name: "dueDateFrom", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime))]
    [OpenApiParameter(name: "dueDateTo", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime))]
    [OpenApiParameter(name: "sortBy", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
    [OpenApiParameter(name: "sortDescending", In = ParameterLocation.Query, Required = false, Type = typeof(bool))]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(TaskDto[])
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Unauthorized,
        contentType: "application/json",
        bodyType: typeof(object)
    )]
    public async Task<HttpResponseData> GetTasks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks")]
        HttpRequestData req,
        CancellationToken ct
    )
    {
        var principal = AuthMiddleware.ValidateRequest(req, jwtService);
        if (principal is null) return await UnauthorizedAsync(req);

        var userId = AuthMiddleware.GetUserId(principal);
        var query = req.Query;

        var searchTerm = query["search"];
        TaskStatus? status = int.TryParse(query["status"], out var s) ? (TaskStatus)s : null;
        Guid? assignedTo = Guid.TryParse(query["assignedTo"], out var at) ? at : null;
        DateTime? dueDateFrom = DateTime.TryParse(query["dueDateFrom"], out var df) ? df : null;
        DateTime? dueDateTo = DateTime.TryParse(query["dueDateTo"], out var dt) ? dt : null;
        var sortBy = query["sortBy"];
        bool sortDescending = bool.TryParse(query["sortDescending"], out var sd) && sd;

        var hasFilters = !string.IsNullOrEmpty(searchTerm) || status.HasValue || assignedTo.HasValue
                         || dueDateFrom.HasValue || dueDateTo.HasValue || !string.IsNullOrEmpty(sortBy);

        var result = hasFilters
            ? await taskService.SearchAsync(userId, searchTerm, status, assignedTo, dueDateFrom, dueDateTo, sortBy,
                sortDescending, ct)
            : await taskService.GetAllAsync(userId, ct);

        if (!result.IsSuccess)
        {
            logger.LogError("GetTasks failed: {Message}", result.Message);
            return await InternalErrorAsync(req);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result.Data, ct);
        return response;
    }

    [Function("GetTaskById")]
    [OpenApiOperation(operationId: "GetTaskById", tags: ["Tasks"], Summary = "Get task by ID")]
    [JwtAuthorization]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TaskDto))]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(object)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Unauthorized,
        contentType: "application/json",
        bodyType: typeof(object)
    )]
    public async Task<HttpResponseData> GetTaskById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks/{id}")]
        HttpRequestData req,
        string id,
        CancellationToken ct
    )
    {
        var principal = AuthMiddleware.ValidateRequest(req, jwtService);
        if (principal is null) return await UnauthorizedAsync(req);

        if (!Guid.TryParse(id, out var taskId))
            return await BadRequestAsync(req, "Invalid task ID");

        var userId = AuthMiddleware.GetUserId(principal);
        var result = await taskService.GetByIdAsync(taskId, userId, ct);

        if (!result.IsSuccess)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteAsJsonAsync(new { error = result.Message }, ct);
            return response;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(result.Data, ct);
        return ok;
    }

    [Function("CreateTask")]
    [OpenApiOperation(operationId: "CreateTask", tags: ["Tasks"], Summary = "Create a new task")]
    [JwtAuthorization]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateTaskDto), Required = true)]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Created,
        contentType: "application/json",
        bodyType: typeof(TaskDto)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.BadRequest,
        contentType: "application/json",
        bodyType: typeof(object)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Unauthorized,
        contentType: "application/json",
        bodyType: typeof(object)
    )]
    public async Task<HttpResponseData> CreateTask(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tasks")]
        HttpRequestData req,
        CancellationToken ct
    )
    {
        var principal = AuthMiddleware.ValidateRequest(req, jwtService);
        if (principal is null) return await UnauthorizedAsync(req);

        var userId = AuthMiddleware.GetUserId(principal);

        CreateTaskDto? dto;
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(ct);
            dto = JsonSerializer.Deserialize<CreateTaskDto>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return await BadRequestAsync(req, "Invalid request body");
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.Title))
            return await BadRequestAsync(req, "Title is required");

        var result = await taskService.CreateAsync(dto with { CreatedBy = userId }, ct);

        if (!result.IsSuccess)
        {
            logger.LogError("CreateTask failed: {Message}", result.Message);
            return await InternalErrorAsync(req);
        }

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(result.Data, ct);
        return response;
    }

    [Function("UpdateTask")]
    [OpenApiOperation(operationId: "UpdateTask", tags: ["Tasks"], Summary = "Update an existing task")]
    [JwtAuthorization]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateTaskDto), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TaskDto))]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(object)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Unauthorized,
        contentType: "application/json",
        bodyType: typeof(object)
    )]
    public async Task<HttpResponseData> UpdateTask(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "tasks/{id}")]
        HttpRequestData req,
        string id,
        CancellationToken ct
    )
    {
        var principal = AuthMiddleware.ValidateRequest(req, jwtService);
        if (principal is null) return await UnauthorizedAsync(req);

        if (!Guid.TryParse(id, out var taskId))
            return await BadRequestAsync(req, "Invalid task ID");

        var userId = AuthMiddleware.GetUserId(principal);

        UpdateTaskDto? dto;
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(ct);
            dto = JsonSerializer.Deserialize<UpdateTaskDto>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return await BadRequestAsync(req, "Invalid request body");
        }

        if (dto is null)
            return await BadRequestAsync(req, "Invalid data");

        var result = await taskService.UpdateAsync(dto with { Id = taskId, UpdatedBy = userId }, ct);

        if (!result.IsSuccess)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteAsJsonAsync(new { error = result.Message }, ct);
            return response;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(result.Data, ct);
        return ok;
    }

    [Function("DeleteTask")]
    [OpenApiOperation(operationId: "DeleteTask", tags: ["Tasks"], Summary = "Delete a task")]
    [JwtAuthorization]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NoContent,
        contentType: "application/json",
        bodyType: typeof(void)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(object)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Unauthorized,
        contentType: "application/json",
        bodyType: typeof(object)
    )]
    public async Task<HttpResponseData> DeleteTask(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tasks/{id}")]
        HttpRequestData req,
        string id,
        CancellationToken ct
    )
    {
        var principal = AuthMiddleware.ValidateRequest(req, jwtService);
        if (principal is null) return await UnauthorizedAsync(req);

        if (!Guid.TryParse(id, out var taskId))
            return await BadRequestAsync(req, "Invalid task ID");

        var userId = AuthMiddleware.GetUserId(principal);
        var result = await taskService.DeleteAsync(taskId, userId, ct);

        if (!result.IsSuccess)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteAsJsonAsync(new { error = result.Message }, ct);
            return response;
        }

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    private static async Task<HttpResponseData> UnauthorizedAsync(HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(new { error = "Unauthorized" });
        return response;
    }

    private static async Task<HttpResponseData> BadRequestAsync(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }

    private static async Task<HttpResponseData> InternalErrorAsync(HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        await response.WriteAsJsonAsync(new { error = "Internal server error" });
        return response;
    }
}
