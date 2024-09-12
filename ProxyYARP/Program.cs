namespace ProxyYARP;

public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IServiceCollection services   = builder.Services;

        // Add services to the container
        {
            services.AddAuthorization();
        }

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        {
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapGet("/", () => "Hello YARP!");
        }

        app.Run();
    }
}
