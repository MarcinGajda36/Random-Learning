using DITesting.Background;

namespace DITesting;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        _ = builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        _ = builder.Services.AddEndpointsApiExplorer();
        _ = builder.Services.AddSwaggerGen();
        _ = builder.Services.AddSingleton<ClientFactory<object>>((url, client) => new { url, client });
        _ = builder.Services.AddSingleton<UtcNow>(() => DateTimeOffset.UtcNow);
        //_ = builder.Services.AddSingleton<UtcNow2>(); // Fails
        _ = builder.Services.AddHostedService<BackgroundSomething>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            _ = app.UseSwagger();
            _ = app.UseSwaggerUI();
        }

        _ = app.UseHttpsRedirection();

        _ = app.UseAuthorization();

        _ = app.MapControllers();

        app.Run();
    }
}
