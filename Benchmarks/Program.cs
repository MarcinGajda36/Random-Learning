using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class Program
{
    public static Task Main(string[] args)
    {
        //var s = new HistoricalToLiveBenchmark_Halves();
        //s.ElementsCount = 1000;
        //await s.HistoricalToLive2_Mutable();
        BenchmarkRunner.Run<HistoricalToLiveBenchmark_Halves>();
        return Task.CompletedTask;
    }
}