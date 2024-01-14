using IISHFTest.Core.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace IISHFTest.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private const string API_KEY_HEADER_NAME = "X-API-KEY";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var apiKeySettings = context.HttpContext.RequestServices.GetService<ApiKeySettings>();
        var apiKey = apiKeySettings.SecretKey;

        if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out var extractedApiKey))
        {
            context.Result = new ContentResult()
            {
                StatusCode = 401,
                Content = "API Key was not provided.",
                ContentType = "text/plain"
            };
        }
        else if (!apiKey.Equals(extractedApiKey))
        {
            context.Result = new ContentResult()
            {
                StatusCode = 403,
                Content = "Unauthorized client.",
                ContentType = "text/plain"
            };
        }
    }
}