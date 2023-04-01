using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class Program
{
    //PS C:\Users\kirgo\source\repos\Random-Learning\Benchmarks> dotnet run -c Release
    public static Task Main(string[] args)
    {
        BenchmarkRunner.Run<PoolsBenchmarks>();
        return Task.CompletedTask;
    }
}