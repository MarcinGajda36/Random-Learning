namespace OrleansSilo01;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Program
{
    static async Task Main(string[] args)
    {
        var hostBuilder = Host
            .CreateDefaultBuilder(args)
            .UseOrleans(silo
                => silo
                    .UseLocalhostClustering()
                    .ConfigureLogging(logging => logging.AddConsole()))
            .UseConsoleLifetime();

        using var host = hostBuilder.Build();
        await host.RunAsync();
    }
}
