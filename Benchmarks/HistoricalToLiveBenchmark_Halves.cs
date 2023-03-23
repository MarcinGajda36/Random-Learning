using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using LanguageExt;
using MarcinGajda.RXTests;

namespace Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class HistoricalToLiveBenchmark_Halves
{

    [Params(10_000)]
    public int ElementsCount { get; set; }

    const int LastValue = int.MaxValue;
    private Subject<int> Live { get; set; } = new();
    private Subject<int> Historical { get; set; } = new();

    [IterationSetup]
    public void SetUp()
    {
        Live?.Dispose();
        Live = new Subject<int>();

        Historical?.Dispose();
        Historical = new Subject<int>();
    }

    readonly record struct HistoricalLivePair(IObservable<int> Historical, IObservable<int> Live);
    Task WaitFor2LastValues(Func<HistoricalLivePair, IObservable<int>> subscribtion)
    {
        var task = subscribtion(new(Historical, Live))
            .Where(x => x == LastValue)
            .Take(2)
            .ToTask();

        int half = ElementsCount / 2;
        for (int i = 0; i < half; i++)
        {
            Historical.OnNext(i);
        }
        for (int i = 0; i < half; i++)
        {
            Live.OnNext(i);
        }
        for (int i = half; i < ElementsCount; i++)
        {
            Historical.OnNext(i);
        }
        Historical.OnNext(LastValue);
        Historical.OnCompleted();

        for (int i = half; i < ElementsCount; i++)
        {
            Live.OnNext(i);
        }

        Live.OnNext(LastValue);

        return task;
    }

    [Benchmark]
    public Task HistoricalToLive2_Immutable()
    {
        return WaitFor2LastValues(pair => HistoricalToLive.ConcatLiveAfterHistory(pair.Live, pair.Historical));
    }

    [Benchmark]
    public Task HistoricalToLive2_Mutable()
    {
        return WaitFor2LastValues(pair => HistoricalToLive2.ConcatLiveAfterHistory(pair.Live, pair.Historical));
    }
}
