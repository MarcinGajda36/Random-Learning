﻿using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using LanguageExt;
using MarcinGajda.RX_IX_Tests;

namespace Benchmarks;

[MemoryDiagnoser]
[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
public class HistoricalToLiveBenchmark_Halves
{
    [Params(10_000, 250_000, 1_000_000)]
    public int ElementsCount { get; set; }

    const int LastIntValue = int.MaxValue;
    const int NormalIntValue = 1;
    private Subject<int> LiveInts { get; set; } = new();
    private Subject<int> HistoricalInts { get; set; } = new();

    //[IterationSetup(Targets = new[] { nameof(HistoricalToLive_IList_Ints) })]
    //public void SetUpInts()
    //{
    //    LiveInts?.Dispose();
    //    LiveInts = new Subject<int>();

    //    HistoricalInts?.Dispose();
    //    HistoricalInts = new Subject<int>();
    //}

    const string LastStringValue = "Last";
    const string NormalStringValue = "Normal";
    private Subject<string> LiveStrings { get; set; } = new();
    private Subject<string> HistoricalStrings { get; set; } = new();

    [IterationSetup(Targets = new[] { nameof(HistoricalToLive_Casting_Strings) })]
    public void SetUpStrings()
    {
        LiveStrings?.Dispose();
        LiveStrings = new();

        HistoricalStrings?.Dispose();
        HistoricalStrings = new();
    }

    readonly record struct HistoricalLivePair<T>(Subject<T> Historical, Subject<T> Live);
    async Task WaitFor2LastValues<T>(HistoricalLivePair<T> pair, Func<HistoricalLivePair<T>, IObservable<T>> subscription, T normal, T last)
    {
        var (historical, live) = pair;
        var task = subscription(pair)
            .Where(seen => EqualityComparer<T>.Default.Equals(seen, last))
            .Take(2)
            .ToTask();

        int half = ElementsCount / 2;
        for (int i = 0; i < half; i++)
        {
            historical.OnNext(normal);
        }
        for (int i = 0; i < half; i++)
        {
            live.OnNext(normal);
        }
        for (int i = half; i < ElementsCount; i++)
        {
            historical.OnNext(normal);
        }
        historical.OnNext(last);
        historical.OnCompleted();

        for (int i = half; i < ElementsCount; i++)
        {
            live.OnNext(normal);
        }

        live.OnNext(last);

        await task;
    }

    //[Benchmark]
    //public async Task HistoricalToLive_Casting_Ints()
    //{
    //    await WaitFor2LastValues(
    //        new(HistoricalInts, LiveInts),
    //        pair => HistoricalToLive_Casting.ConcatLiveAfterHistory(pair.Live, pair.Historical),
    //        NormalIntValue,
    //        LastIntValue);
    //}

    [Benchmark]
    public async Task HistoricalToLive_Casting_Strings()
    {
        await WaitFor2LastValues(
            new(HistoricalStrings, LiveStrings),
            pair => HistoricalToLive_Casting.ConcatLiveAfterHistory(pair.Live, pair.Historical),
            NormalStringValue,
            LastStringValue);
    }
}
