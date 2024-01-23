using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MarcinGajda.AsyncDispose_;
using MarcinGajda.Collections;
using MarcinGajda.ContractsT;
using MarcinGajda.Copy;
using MarcinGajda.DataflowTests;
using MarcinGajda.PeriodicCheckers;
using MarcinGajda.RX_IX_Tests;
using MarcinGajda.Structs;
using MarcinGajda.Synchronization.Pooling;
using MarcinGajda.WORK_observable;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MarcinGajda;

internal class Program
{
    private const int i = 1 << 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (TimeSpan, TResult) Measure<TResult>(Func<TResult> toMeasure)
    {
        long start = Stopwatch.GetTimestamp();
        var result = toMeasure();
        long elapsed = Stopwatch.GetTimestamp() - start;
        return (TimeSpan.FromTicks(elapsed), result);
    }

    public static Span<int> ReturnsSpan()
    {
        return new int[] { 1, 2, 3, 4, 5 };
    }

    public static void TestSpanAfterGC()
    {
        Span<int> spanIntoArr = ReturnsSpan();
        GC.Collect();
        Thread.Sleep(1000);
        Thread.Sleep(1000);
    }

    public static async Task Main()
    {
        TestSpanAfterGC();
        PoolTest();
        await Task.Delay(-1);
        _ = TestClosure();
        await Observables.LinqQueryTests();

        await TestEviction();

        await BroadcastBlockTest.Test();

        await Observables.AndThenWhen();

        await Observables.ObservableBuff();
        await Observables.ObservableColections();
        await WorkObservable.TestSplit(default);

        Observables.TestQ();

        Observables.Windows();
        Observables.Schedulerss();
        Observables.Generate();
        Observables.Throws();
        Observables.SubjectTest1();
        Observables.AsyncSubjectTest();
        Observables.BehaviorSubjectTest();
        Observables.SubjectTest();
        Observables.ReplaySubjectTest();

        SortedSetTst.Test();
        _ = ContractsTests.Test(-1);

        await AsyncEnumerableToBlock.AsyncEnumeratorConsumer();
        await AsyncDisposableTests.Test();
        ArraysTests.Test1();
        await CopyingTest.CopyFileAsync(@"C:\Users\kirgo\Desktop\CV\Profile1.pdf", @"C:\Users\kirgo\Desktop\CV\Profile2.pdf");
        //await ObservableChecker.TestBasic1();
        //ReadOnlyRefStruct.ReadOnlyRefStructTEst();
        //await DataflowLoggerTests.Test1Async();

        await ObservableChecker.TestBasic1();
        _ = new WeakTableProd().LangDictionary("", "");
        _ = new int[0].Any();
        _ = (new int[1])[..1];
        _ = new int[] { 1 }.Any();
        _ = ((int[])null).Any();

        _ = string.Equals("111", "asd", StringComparison.OrdinalIgnoreCase);
        FizzBuzzKata.Print();
        FizzBuzzKata.FizzBuzzMy();

        string abc = "abc";
        _ = ImmutableTests.Increment(abc);

        var point = new Point(1, 2);
        (double _, double _) = point;
        _ = await Func();
        _ = ImmutableList.Create(1, 2, 3);
        _ = TestAsyncException(2);
        (_, _, _, int pop1, _, int pop2) = QueryCityDataForYears("New York City", 1960, 2010);
        Console.WriteLine($"Population change, 1960 to 2010: {pop2 - pop1:N0}");
        ParrallelTests();
    }

    private static void PoolTest()
    {
        var pool = new ThreadStaticPool<object>(() => new object());
        var lease1 = pool.RentLease();
        var lease2 = pool.RentLease();
        lease1.Dispose();
        var lease3 = pool.RentLease();
        lease2.Dispose();
        lease3.Dispose();
    }

    public static int TestClosure()
    {
        int i = 1;
        var capturesI = new Lazy<int>(() => i);
        i = 2;
        Console.WriteLine(capturesI.Value);
        return capturesI.Value;
    }

    private static async Task TestEviction()
    {
        using var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromSeconds(3) }));
        _ = memoryCache.GetOrCreate("xd", entry =>
        {
            _ = entry.RegisterPostEvictionCallback((key, value, _, _) =>
            {
                Console.WriteLine("aaaaa");
            });
            _ = entry.SetSlidingExpiration(TimeSpan.FromSeconds(2));
            return "a";
        });
        await Task.Delay(-1);
    }

    private static async Task TestAsyncRef(int x)
    {
        static int X(ref int x1) => x1;
        _ = X(ref x);
    }

    private static void ParrallelTests()
        => new int[] { 1, 2, 3 }
        .AsParallel()
        .WithDegreeOfParallelism(Environment.ProcessorCount)
        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
        .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
        .WithCancellation(default)
        .AsOrdered()
        .AsUnordered()
        .AsSequential();

    private static (string, double, int, int, int, int) QueryCityDataForYears(string name, int year1, int year2)
    {
        int population1 = 0, population2 = 0;
        double area = 0;

        if (name == "New York City")
        {
            area = 468.48;
            if (year1 == 1960)
            {
                population1 = 7781984;
            }
            if (year2 == 2010)
            {
                population2 = 8175133;
            }
            return (name, area, year1, population1, year2, population2);
        }

        return ("", 0, 0, 0, 0, 0);
    }
    public static Task<int> TestAsyncException(int x)
    {
        var ra = new ValueTask<int>(4);

        throw new Exception(nameof(TestAsyncException));
        //return 5;
        return Body(x);

        Task<int> Body(int copyX) => Task.Run(() => 1 * copyX);
    }
    public static async ValueTask Func1()
    {
        var mem = new Memory<byte>();
        _ = await new FileInfo("").OpenRead().ReadAsync(mem);
    }
    public static async ValueTask<int> Func()
    {
        await Task.Delay(100);
        return 5;
    }
    public static void TestStackalloc(int size)
    {
        int size2 = size;
        byte[,] notLol = new byte[1, 2];
        Span<byte> span = size <= 128 ? stackalloc byte[size] : new byte[size];
        span[0] = default;
        _ = span[1..];
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = 2;
        }
    }
    public static TGeneric GenericTest<TGeneric>(TGeneric generic)
        where TGeneric : Delegate => generic;
    //static async Task Main(string[] args)
    //{

    //    var point = new Point(1, 2);
    //    var (x, y) = point;

    //    await Task.Run(() =>
    //    {
    //        throw new Exception("test map");
    //        return 1;
    //    }).Map(i => $"i: {i}");

    //    WeakRefTest.Test();

    //    var test = new TestClass();

    //    var weakRefToTest = new WeakReference<TestClass>(test);
    //    weakRefToTest.TryGetTarget(out var maybeTest);

    //    var weakRefToObject = new WeakReference(test);
    //    var refToTestAsObject = weakRefToObject.Target;

    //    using (var dispStruct = new DisposableStruct())
    //    {

    //    }

    //    await Task.FromException(new Exception("Test ex1")).ObserveException(Console.WriteLine);
    //    await Task.FromException(new Exception("Test ex3"));

    //    ConcurrentDictionary<int, string> keyValuePairs = new ConcurrentDictionary<int, string>();
    //    if (keyValuePairs.IsEmpty) { }
    //}

    public static int SumPositiveNumbers(IEnumerable<object> sequence)
    {
        int sum = 0;
        foreach (object i in sequence)
        {
            switch (i)
            {
                case 0:
                    break;
                case IEnumerable<int> childSequence:
                    {
                        foreach (int item in childSequence)
                        {
                            sum += (item > 0) ? item : 0;
                        }

                        break;
                    }
                case int n when n > 0:
                    sum += n;
                    break;
                case null:
                    throw new NullReferenceException("Null found in sequence");
                default:
                    throw new InvalidOperationException("Unrecognized type");
            }
        }
        return sum;
    }
    public static IEnumerable<char> AlphabetSubset3(char start, char end)
    {
        if (start is < 'a' or > 'z')
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(start), message: "start must be a letter");
        }

        if (end is < 'a' or > 'z')
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(end), message: "end must be a letter");
        }

        return end <= start
            ? throw new ArgumentException($"{nameof(end)} must be greater than {nameof(start)}")
            : alphabetSubsetImplementation();
        IEnumerable<char> alphabetSubsetImplementation()
        {
            for (char c = start; c < end; c++)
            {
                yield return c;
            }
        }
    }
    public Task<string> PerformLongRunningWork(string address, int index, string name)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException(message: "An address is required", paramName: nameof(address));
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(index), message: "The index must be non-negative");
        }

        return string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException(message: "You must supply a name", paramName: nameof(name))
            : longRunningWorkImplementation();

        static async Task<string> longRunningWorkImplementation() =>
            //var interimResult = await FirstWork(address);
            //var secondResult = await SecondStep(index, name);
            //return $"The results are {interimResult} and {secondResult}. Enjoy.";
            "";
    }
}
