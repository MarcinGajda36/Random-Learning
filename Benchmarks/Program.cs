using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class Program
{
    public static Task Main(string[] args)
    {
        BenchmarkRunner.Run<HistoricalToLiveBenchmark_Halves>();
        return Task.CompletedTask;
    }
}