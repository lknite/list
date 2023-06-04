using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using list.Middleware;
using list.Helpers;
using System.Security.Claims;
using System.Text.Json;

namespace list.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string ApiKey = "X-Api-Key";
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Check if required 'X-Api-Key' header was provided
            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKey, out var apiKeyVal))
            {
                //Console.WriteLine("ApiKeyAttribute (1)");
                context.HttpContext.Response.StatusCode = 401;
                await context.HttpContext.Response.WriteAsync("[a] Api Key was not provided");
                return;
            }

            // Is apikey a valid guid?
            if (!Guid.TryParse(apiKeyVal, out _))
            {
                //Console.WriteLine("ApiKeyAttribute (2)");
                context.HttpContext.Response.StatusCode = 401;
                await context.HttpContext.Response.WriteAsync("[b] Api Key was not valid");
                return;
            }

            // Check if provided apikey exists
            if (! await ApiKeyMiddleware.IsApiKeyValid(apiKeyVal))
            {
                //Console.WriteLine("ApiKeyAttribute (3)");
                context.HttpContext.Response.StatusCode = 401;
                await context.HttpContext.Response.WriteAsync("[c] Unauthorized client");
                return;
            }


            // Add claims via ApiKey
            JsonElement o = JsonSerializer.Deserialize<JsonElement>(
                await zApiToken.Identify(Guid.Parse(apiKeyVal), Globals.service.kubeconfig.Namespace));

            var claims = new List<Claim>();
            foreach (JsonProperty property in o.EnumerateObject())
            {
                claims.Add(new Claim(property.Name, property.Value.ToString()));
            }

            var appIdentity = new ClaimsIdentity(claims);
            context.HttpContext.User.AddIdentity(appIdentity);
        }
    }
}
