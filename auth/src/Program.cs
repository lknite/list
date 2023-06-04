using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;

namespace lido
{
    public static class Globals
    {
        public static Service service = new Service();
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            // adding this here will cause the compiler to ensure globals is fully instantiated before proceeding
            Globals.service.Start();

            var builder = WebApplication.CreateBuilder(args);


            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "list",
                    Description = "",
                    TermsOfService = new Uri("https://travisloyd.xyz/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Travis Loyd",
                        Url = new Uri("https://travisloyd.xyz")
                    }
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme.<br><br>
                      Enter 'Bearer' [space] and then your token in the text input below.
                      <br><br>Example: 'Bearer 4d947de29ba54093a8ba57d5c9ed18764d947de29ba54093a8ba57d5c9ed1876=='",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement() {{
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }});

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            /*
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins()
                                .AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                        //builder.WithOrigins("http://example.com",
                        //                    "http://www.contoso.com");
                    });
            });
            */

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            IdentityModelEventSource.ShowPII = true;

            builder.Services.AddAuthentication(options => {

                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // use always when using https
                //options.Cookie.SameSite = SameSiteMode.Lax;
                //options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            })
            .AddOpenIdConnect(options => {

                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.Authority = Environment.GetEnvironmentVariable("OIDC_ENDPOINT");
                options.ClientId = Environment.GetEnvironmentVariable("OIDC_CLIENT_ID");
                options.ClientSecret = Environment.GetEnvironmentVariable("OIDC_CLIENT_SECRET");
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.CallbackPath = Environment.GetEnvironmentVariable("OIDC_CALLBACK");
                options.GetClaimsFromUserInfoEndpoint = true;

                string scopeString = Environment.GetEnvironmentVariable("OIDC_SCOPE");
                scopeString.Split(" ", StringSplitOptions.TrimEntries).ToList().ForEach(scope => {
                    options.Scope.Add(scope);
                });

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = options.Authority,
                    ValidAudience = options.ClientId
                };

                options.Events.OnRedirectToIdentityProviderForSignOut = (context) =>
                {
                    //context.ProtocolMessage.PostLogoutRedirectUri = "https://gge.vc-non.k.home.net";
                    return Task.CompletedTask;
                };

                options.SaveTokens = true;
            });


            // Set listen port to 80
            builder.WebHost.UseKestrel();
            builder.WebHost.UseUrls("http://*:80");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
//            if (app.Environment.IsDevelopment())
//            {
                app.UseSwagger(c =>
                {
                    c.RouteTemplate = "swagger/auth/{documentName}/swagger.json";
                });
                app.UseSwaggerUI(c => {
                    c.SwaggerEndpoint("/swagger/auth/v1/swagger.json", "My API V1"); 
                    c.RoutePrefix = "swagger/auth"; 
                });

                // This is required when developing using 'http' & 'localhost'
                app.UseCookiePolicy(new CookiePolicyOptions()
                {
                    MinimumSameSitePolicy = SameSiteMode.Lax
                });
//            }

            //app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            //app.UseWebSockets();

            // Handle incoming webrequests in order to start WebSockets
            //app.Use(async (context, next) => { await Globals.service.HandleWebRequest(context, next); });

            app.Run();
        }
    }
}
