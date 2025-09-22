using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class Program
{
    //PS C:\Code\Random-Learning\Benchmarks> dotnet run -c Release
    // run PS as admin
    // folder: C:\Code\Random-Learning\Benchmarks
    // example command: dotnet run -c Release -f net8.0
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<PoolsBenchmarks>();
    }
}