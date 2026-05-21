using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace TaskApp.Api.OpenApi;

public class JwtAuthorizationAttribute : OpenApiSecurityAttribute
{
    public JwtAuthorizationAttribute() : base("bearer_auth", SecuritySchemeType.Http)
    {
        this.Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below.";
        this.In = OpenApiSecurityLocationType.Header;
        this.Scheme = OpenApiSecuritySchemeType.Bearer;
        this.BearerFormat = "JWT";
    }
}
