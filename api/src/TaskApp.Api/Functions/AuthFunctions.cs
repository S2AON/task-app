using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using TaskApp.Application.Dtos;
using TaskApp.Application.Interfaces;

namespace TaskApp.Api.Functions;

public class AuthFunctions(ILogger<AuthFunctions> logger, IAuthService authService)
{
    [Function("Register")]
    [OpenApiOperation(operationId: "Register", tags: ["Authentication"], Summary = "Register a new user")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RegisterDto), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(AuthResponseDto))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req,
        CancellationToken ct)
    {
        RegisterDto? dto;
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(ct);
            dto = JsonSerializer.Deserialize<RegisterDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return await ErrorAsync(req, HttpStatusCode.BadRequest, "Invalid request body");
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return await ErrorAsync(req, HttpStatusCode.BadRequest, "Email and password are required");

        var result = await authService.RegisterAsync(dto, ct);

        if (!result.IsSuccess)
        {
            var statusCode = result.Message == "Email already registered"
                ? HttpStatusCode.Conflict
                : HttpStatusCode.BadRequest;
            return await ErrorAsync(req, statusCode, result.Message!);
        }

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(result.Data, ct);
        return response;
    }

    [Function("GoogleLogin")]
    [OpenApiOperation(operationId: "GoogleLogin", tags: ["Authentication"], Summary = "Sign in with Google")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(GoogleLoginDto), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthResponseDto))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> GoogleLogin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/google")] HttpRequestData req,
        CancellationToken ct)
    {
        GoogleLoginDto? dto;
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(ct);
            dto = JsonSerializer.Deserialize<GoogleLoginDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return await ErrorAsync(req, HttpStatusCode.BadRequest, "Invalid request body");
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.IdToken))
            return await ErrorAsync(req, HttpStatusCode.BadRequest, "Google ID token is required");

        var result = await authService.GoogleLoginAsync(dto, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Google login failed");
            return await ErrorAsync(req, HttpStatusCode.Unauthorized, result.Message!);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result.Data, ct);
        return response;
    }

    [Function("Login")]
    [OpenApiOperation(operationId: "Login", tags: ["Authentication"], Summary = "User login")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LoginDto), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthResponseDto))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req,
        CancellationToken ct)
    {
        LoginDto? dto;
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(ct);
            dto = JsonSerializer.Deserialize<LoginDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return await ErrorAsync(req, HttpStatusCode.BadRequest, "Invalid request body");
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return await ErrorAsync(req, HttpStatusCode.BadRequest, "Email and password are required");

        var result = await authService.LoginAsync(dto, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed login attempt for {Email}", dto.Email);
            return await ErrorAsync(req, HttpStatusCode.Unauthorized, "Invalid credentials");
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result.Data, ct);
        return response;
    }

    private static async Task<HttpResponseData> ErrorAsync(HttpRequestData req, HttpStatusCode status, string message)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}
