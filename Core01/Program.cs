﻿using MarcinGajda.AsyncDispose_;
using MarcinGajda.Collections;
using MarcinGajda.ContractsT;
using MarcinGajda.Copy;
using MarcinGajda.DataflowTests;
using MarcinGajda.Fs;
using MarcinGajda.PeriodicCheckers;
using MarcinGajda.RXTests;
using MarcinGajda.Structs;
using MarcinGajda.WORK_observable;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MarcinGajda
{
    internal class Program
    {
        private const int i = 1 << 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (TimeSpan, TResult) Measure<TResult>(Func<TResult> toTest)
        {
            var start = Stopwatch.GetTimestamp();
            TResult result = toTest();
            var elapsed = Stopwatch.GetTimestamp() - start;
            return (TimeSpan.FromTicks(elapsed), result);
        }


        public static async Task Main()
        {
            await Observables.LinqQueryTests();

            await Task.Delay(-1);
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

            await FTypesSharing.SameSlnShare();

            SortedSetTst.Test();
            int r = ContractsTests.Test(-1);

            await AsyncEnumerableToBlock.AsyncEnumeratorConsumer();
            await AsyncDisposableTests.Test();
            ArraysTests.Test1();
            await CopyingTest.CopyFileAsync(@"C:\Users\kirgo\Desktop\CV\Profile1.pdf", @"C:\Users\kirgo\Desktop\CV\Profile2.pdf");
            //await ObservableChecker.TestBasic1();
            //ReadOnlyRefStruct.ReadOnlyRefStructTEst();
            //await DataflowLoggerTests.Test1Async();

            await ObservableChecker.TestBasic1();
            Task<Dictionary<string, string>> dict = new WeakTableProd().LangDictionary("", "");

            bool isEmpty = new int[0].Any();
            int[] adasdasd = (new int[1])[..1];
            bool isEmpty1 = new int[] { 1 }.Any();
            bool isEmpty2 = ((int[])null).Any();

            FTypesSharing.Test();
            _ = string.Equals("111", "asd", StringComparison.OrdinalIgnoreCase);
            FizzBuzzKata.Print();
            FizzBuzzKata.FizzBuzzMy();

            string abc = "abc";
            _ = ImmutableTests.Increment(abc);

            var point = new Point(1, 2);
            (double x, double y) = point;
            int fromValTask = await Func();
            var list = ImmutableList.Create(1, 2, 3);
            Task<int> testEx = TestAsyncException(2);
            (string _, double _, int _, int pop1, int _, int pop2) = QueryCityDataForYears("New York City", 1960, 2010);
            Console.WriteLine($"Population change, 1960 to 2010: {pop2 - pop1:N0}");
            ParrallelTests();
        }

        private static async Task TestEviction()
        {
            using MemoryCache memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromSeconds(3) }));
            memoryCache.GetOrCreate("xd", entry =>
            {
                entry.RegisterPostEvictionCallback((key, value, _, _) =>
                {
                    Console.WriteLine("aaaaa");
                });
                entry.SetSlidingExpiration(TimeSpan.FromSeconds(2));
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
            span.Slice(1);
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
            if (start < 'a' || start > 'z')
            {
                throw new ArgumentOutOfRangeException(paramName: nameof(start), message: "start must be a letter");
            }

            if (end < 'a' || end > 'z')
            {
                throw new ArgumentOutOfRangeException(paramName: nameof(end), message: "end must be a letter");
            }

            if (end <= start)
            {
                throw new ArgumentException($"{nameof(end)} must be greater than {nameof(start)}");
            }

            return alphabetSubsetImplementation();

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

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(message: "You must supply a name", paramName: nameof(name));
            }

            return longRunningWorkImplementation();

            async Task<string> longRunningWorkImplementation() =>
                //var interimResult = await FirstWork(address);
                //var secondResult = await SecondStep(index, name);
                //return $"The results are {interimResult} and {secondResult}. Enjoy.";
                "";
        }
    }
}
