using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ProxyYARP.Miscellaneous;
using Yarp.ReverseProxy.Transforms;

namespace ProxyYARP;

public static class Program
{
    public const string AuthScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IServiceCollection services   = builder.Services;
        IConfiguration configuration  = builder.Configuration;

        string weatherApiKey = configuration["WeatherApiKey"] ?? string.Empty;

        // Add services to the container
        {
            services.addCookieAuth();

            services.AddReverseProxy()
                .LoadFromConfig(configuration.GetSection("ReverseProxy"))
                .AddTransforms(builderContext =>
                {
                    if (builderContext.Route.RouteId == "route-weather")
                    {
                        builderContext.AddQueryValue("key", weatherApiKey, append: false);
                    }
                });

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy<string, RateLimiterPolicyByIPAddress>("RateLimiterByIP");
                options.AddPolicy<string, RateLimiterPolicyByUser>("RateLimiterByUser");
            });
        }

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        {
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.MapGet("/", () => "Hello YARP!").RequireRateLimiting("RateLimiterByIP");

            app.MapReverseProxy();

            app.MapAuthEndpoints();
        }

        app.Run();
    }

    private static void addCookieAuth(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("weather-auth-policy", builder => builder.RequireAuthenticatedUser());

        services.AddAuthentication(AuthScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "auth-cookie";
                options.Events.OnRedirectToLogin = preventRedirect;
            });
    }

    private static Task preventRedirect(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        return Task.CompletedTask;
    }
}
