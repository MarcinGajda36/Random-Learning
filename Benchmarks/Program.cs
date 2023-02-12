using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class Program
{
    private static void Main(string[] args)
        => BenchmarkRunner.Run<HistoricalToLiveBenchmark_Halves>();
}