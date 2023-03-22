using System.Reactive.Linq;
using System.Reactive.Subjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MarcinGajda.RXTests;

namespace Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class HistoricalToLiveBenchmark_Halves
{
    private const int ElementsCount = 4096;

    //[Benchmark]
    //public void HistoricalToLive_UnionAndImmutable()
    //{
    //    using var live = new Subject<int>();
    //    using var historical = new Subject<int>();

    //    using var merge = HistoricalToLive
    //        .ConcatLiveAfterHistory(live, historical)
    //        .Subscribe();

    //    int half = ElementsCount / 2;
    //    for (int i = 0; i < half; i++)
    //    {
    //        live.OnNext(i);
    //    }
    //    for (int i = 0; i < ElementsCount; i++)
    //    {
    //        historical.OnNext(i);
    //    }
    //    historical.OnCompleted();
    //    for (int i = half; i < ElementsCount; i++)
    //    {
    //        live.OnNext(i);
    //    }
    //}

    [Benchmark]
    public void HistoricalToLive2_StructAndMutation()
    {
        using var live = new Subject<int>();
        using var historical = new Subject<int>();

        using var merge = HistoricalToLive2
            .ConcatLiveAfterHistory(live, historical)
            .Subscribe();

        int half = ElementsCount / 2;
        for (int i = 0; i < half; i++)
        {
            live.OnNext(i);
        }
        for (int i = 0; i < ElementsCount; i++)
        {
            historical.OnNext(i);
        }
        historical.OnCompleted();
        for (int i = half; i < ElementsCount; i++)
        {
            live.OnNext(i);
        }
    }
}
