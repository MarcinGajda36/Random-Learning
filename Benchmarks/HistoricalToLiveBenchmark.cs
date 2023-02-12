using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MarcinGajda.RXTests;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class HistoricalToLiveBenchmark
{
    private const int ElementsCount = 1000;
    private Subject<int> live;
    private int[] historical;

    [GlobalSetup]
    public void Setup()
    {
        live = new Subject<int>();
        historical = new int[ElementsCount];
        for (int i = 0; i < historical.Length; i++)
        {
            historical[i] = i;
        }
    }

    [Benchmark]
    public void HistoricalToLive_UnionAndImmutable()
    {
        using var merge = HistoricalToLive
            .ConcatLiveAfterHistory(live, Observable.ToObservable(historical))
            .Subscribe();

        for (int i = 0; i < ElementsCount; i++)
        {
            live.OnNext(i);
        }
    }

    [Benchmark]
    public void HistoricalToLive2_StructAndMutation()
    {
        using var merge = HistoricalToLive2
            .ConcatLiveAfterHistory(live, Observable.ToObservable(historical))
            .Subscribe();

        for (int i = 0; i < ElementsCount; i++)
        {
            live.OnNext(i);
        }
    }
}
