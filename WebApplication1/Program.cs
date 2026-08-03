
namespace WebApplication1;

using System.Threading.RateLimiting;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        _ = builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        _ = builder.Services.AddOpenApi();

        _ = builder.Services.AddRateLimiter(
            options => options.GlobalLimiter = PartitionedRateLimiter.Create(
                (HttpContext key) => RateLimitPartition.GetConcurrencyLimiter(
                    key,
                    key => new ConcurrencyLimiterOptions { PermitLimit = 100, QueueLimit = 200 })));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            _ = app.MapOpenApi();
        }
        _ = app.UseRateLimiter();// new RateLimiterOptions { }
        _ = app.UseHttpsRedirection();
        _ = app.UseAuthorization();

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        _ = app.MapGet("/weatherforecast", (HttpContext httpContext) =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                })
                .ToArray();
            return forecast;
        })
        .WithName("GetWeatherForecast");

        app.Run();
    }
}
