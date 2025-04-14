namespace OrleansClient01;

using System;
using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;

internal class ProgramClient
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

        var line = string.Empty;
        while (line != "quit")
        {
            Console.Write("Provide text: ");
            line = Console.ReadLine() ?? string.Empty;
            var response = await helloGrain.SayHello(line);
            Console.WriteLine(
                $"""
                Response: {response}
                """);
        }

        await host.StopAsync();
    }
}
