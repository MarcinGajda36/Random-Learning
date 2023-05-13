using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class Program
{
    //PS C:\Users\kirgo\source\repos\Random-Learning\Benchmarks> dotnet run -c Release
    public static async Task Main(string[] args)
    {
        //var benchmarks = new HistoricalToLiveBenchmark_Halves()
        //{
        //    ElementsCount = 1_000_000,
        //};
        //await benchmarks.HistoricalToLive2_V2_Mutable_Ints();
        //await benchmarks.HistoricalToLive2_V2_Strings();

        BenchmarkRunner.Run<Concats>();
        await Task.CompletedTask;
    }
}