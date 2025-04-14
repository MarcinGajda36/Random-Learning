namespace OrleansSilo01;

using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class ProgramSilo
{
    static async Task Main(string[] args)
    {
        var hostBuilder = Host
            .CreateDefaultBuilder(args)
            .UseOrleans(silo
                => silo
                    .UseLocalhostClustering()
                    .ConfigureLogging(logging => logging.AddConsole()))
                    //.AddFileGrainStorage("File", options =>
                    //{
                    //    string path = Environment.GetFolderPath(
                    //        Environment.SpecialFolder.ApplicationData);

                    //    options.RootDirectory = Path.Combine(path, "Orleans/GrainState/v1");
                    //})
            .UseConsoleLifetime();

        using var host = hostBuilder.Build();
        await host.RunAsync();
    }
}
