namespace ProxyYARP;

public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IServiceCollection services   = builder.Services;
        IConfiguration configuration  = builder.Configuration;

        // Add services to the container
        {
            services.AddAuthorization();

            services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));
        }

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        {
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapGet("/", () => "Hello YARP!");

            app.MapReverseProxy();
        }

        app.Run();
    }
}
