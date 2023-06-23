using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class Program
{
    //PS C:\Users\kirgo\source\repos\Random-Learning\Benchmarks> dotnet run -c Release
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<HistoricalToLiveBenchmark_Halves>();
    }
}