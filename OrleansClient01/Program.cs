namespace OrleansClient01;

using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Program
{
    static async Task Main(string[] args)
    {
        var hostBuilder = Host
            .CreateDefaultBuilder(args)
            .UseOrleansClient(client => client.UseLocalhostClustering())
            .ConfigureLogging(logging => logging.AddConsole())
            .UseConsoleLifetime();

        using var host = hostBuilder.Build();
        await host.StartAsync();

        IClusterClient client = host.Services.GetRequiredService<IClusterClient>();
        IHello helloGrain = client.GetGrain<IHello>(0);
        var response = await helloGrain.SayHello("Hi from Marcin!");
        Console.WriteLine(
            $"""
            Response: {response}
            """);
        Console.ReadKey();
        await host.StopAsync();
    }
}
