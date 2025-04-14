namespace OrleansSilo01;

using System;
using System.IO;
using System.Threading.Tasks;
using Grains.GrainStorage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;

internal class ProgramSilo
{
    static async Task Main(string[] args)
    {
        var hostBuilder = Host
            .CreateDefaultBuilder(args)
            .UseOrleans(silo
                => silo
                    .UseLocalhostClustering()
                    .ConfigureLogging(logging => logging.AddConsole())
                    .AddFileGrainStorage("File", options =>
                    {
                        string path = Environment.GetFolderPath(
                            Environment.SpecialFolder.ApplicationData);

                        options.RootDirectory = Path.Combine(path, "Orleans/GrainState/v1");
                    }))
            .UseConsoleLifetime();

        using var host = hostBuilder.Build();
        await host.RunAsync();
    }
}
