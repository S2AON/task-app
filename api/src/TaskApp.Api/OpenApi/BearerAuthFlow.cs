using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;

namespace TaskApp.Api.OpenApi;

public class BearerAuthFlow : OpenApiOAuthSecurityFlows
{
    public BearerAuthFlow()
    {
        this.Implicit = new OpenApiOAuthFlow()
        {
            AuthorizationUrl = new System.Uri("https://localhost:7071/api/auth/login"),
            Scopes = { }
        };
    }
}
