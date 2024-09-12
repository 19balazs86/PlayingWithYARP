using ProxyYARP.Miscellaneous;
using Yarp.ReverseProxy.Transforms;

namespace ProxyYARP;

public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IServiceCollection services   = builder.Services;
        IConfiguration configuration  = builder.Configuration;

        string weatherApiKey = configuration["WeatherApiKey"] ?? string.Empty;

        // Add services to the container
        {
            services.AddAuthorization();

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

                options.AddPolicy<string, RateLimiterPolicyByIPAddress>("SlidingWindowByIP");
            });
        }

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        {
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseRateLimiter();

            app.MapGet("/", () => "Hello YARP!").RequireRateLimiting("SlidingWindowByIP");

            app.MapReverseProxy();
        }

        app.Run();
    }
}
