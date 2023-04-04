using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using LanguageExt;
using MarcinGajda.RX_IX_Tests;
using MarcinGajda.RXTests;

namespace Benchmarks;

//[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
[MemoryDiagnoser]
public class HistoricalToLiveBenchmark_Halves
{
    [Params(10_000, 250_000, 1_000_000)]
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
    async Task WaitFor2LastValues(Func<HistoricalLivePair, IObservable<int>> subscribtion)
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

        await task;
    }

    //[Benchmark]
    //public async Task HistoricalToLive2_Immutable()
    //{
    //    await WaitFor2LastValues(pair => HistoricalToLive.ConcatLiveAfterHistory(pair.Live, pair.Historical));
    //}

    [Benchmark]
    public async Task HistoricalToLive2_Mutable()
    {
        await WaitFor2LastValues(pair => HistoricalToLive2.ConcatLiveAfterHistory(pair.Live, pair.Historical));
    }

    [Benchmark]
    public async Task HistoricalToLive3_Mutable()
    {
        await WaitFor2LastValues(pair => HistoricalToLive3.ConcatLiveAfterHistory(pair.Live, pair.Historical).ToObservable());
    }
}
