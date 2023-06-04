using list.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;

namespace list.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string APIKEY = "X-Api-Key";

        // Used on all REST methods if: app.UseMiddleware<ApiKeyMiddleware>();
        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Used on all REST methods if: app.UseMiddleware<ApiKeyMiddleware>();
        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.Value.StartsWith("/swagger")
                && !context.Request.Path.Value.StartsWith("/ws"))
            {
                // Check if required 'XApiKey' header was provided
                if (!context.Request.Headers.TryGetValue(APIKEY, out var extractedApiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("[A] Api Key was not provided");
                    return;
                }

                // Is apikey a valid guid?
                if (!Guid.TryParse(extractedApiKey, out _))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("[B] Api Key was not valid");
                    return;
                }

                // Check if provided apikey exists
                if (!await IsApiKeyValid(extractedApiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("[C] Unauthorized client");
                    return;
                }


                // Add claims via ApiKey
                JsonElement o = JsonSerializer.Deserialize<JsonElement>(
                    await zApiToken.Identify(Guid.Parse(extractedApiKey), Globals.service.kubeconfig.Namespace));

                var claims = new List<Claim>();
                foreach (JsonProperty property in o.EnumerateObject())
                {
                    claims.Add(new Claim(property.Name, property.Value.ToString()));
                }

                var appIdentity = new ClaimsIdentity(claims);
                context.User.AddIdentity(appIdentity);
            }

            await _next(context);
        }

        // Used by ApiKeyAttribute & with web socket
        public static async Task<bool> IsApiKeyValid(string extractedApiKey)
        {
            string claims = string.Empty;
            Guid guid = Guid.Parse(extractedApiKey);

            try
            {
                //
                claims = await zApiToken.Identify(guid, Globals.service.kubeconfig.Namespace);

                // if there is no error, but nothing was returned, the guid is invalid
                if ((claims.Length == 0) || (claims.Length == 2)) // also watch for '{}' or '[]'
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
