using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using MarcinGajda.Synchronizers.Pooling;

namespace Benchmarks;

//[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.CacheMisses)]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PoolsBenchmarks
{
    class RandomType { public int X { get; set; } }
    readonly Func<RandomType> createRandomType = () => new RandomType { X = 5 };

    record ThreadParam<TPool, TLease>(TPool Pool, Func<TPool, TLease> Rent, int N)
        where TLease : struct, IDisposable;

    [Params(250_000)]
    public int Rents { get; set; }

    [Params(1, 2, 8, 16)]
    public int NumberOfThreads { get; set; }

    Thread[]? threads;

    [IterationSetup(Target = nameof(ThreadStatic))]
    public void SetupThreadStatic()
    {
        Setup<ThreadStaticPool<RandomType>, ThreadStaticPool<RandomType>.Lease>();
    }

    [IterationSetup(Target = nameof(Locking))]
    public void SetupLocking()
    {
        Setup<LockingPool<RandomType>, LockingPool<RandomType>.Lease>();
    }

    [IterationSetup(Target = nameof(Spinning))]
    public void SetupSpinning()
    {
        Setup<SpiningPool<RandomType>, SpiningPool<RandomType>.Lease>();
    }

    public void Setup<TPool, TLease>()
        where TLease : struct, IDisposable
    {
        threads = new Thread[NumberOfThreads];
        for (int index = 0; index < NumberOfThreads; index++)
        {
            var thread = new Thread(pool =>
            {
                var typedPool = (ThreadParam<TPool, TLease>)pool!;
                for (int i = 0; i < typedPool.N; i++)
                {
                    using var lease = typedPool.Rent(typedPool.Pool);
                }
            });
            threads[index] = thread;
        }
    }

    public void Test<TPool, TLease>(TPool pool, Func<TPool, TLease> rent)
        where TLease : struct, IDisposable
    {
        var threadParam = new ThreadParam<TPool, TLease>(pool, rent, Rents);
        for (int index = 0; index < threads!.Length; index++)
        {
            threads[index].Start(threadParam);
        }

        Array.ForEach(threads, thread => thread.Join());
    }

    [Benchmark]
    public void ThreadStatic()
    {
        var pool = new ThreadStaticPool<RandomType>(createRandomType);
        Test(pool, static pool => pool.Rent());
    }

    [Benchmark]
    public void Spinning()
    {
        var pool = new SpiningPool<RandomType>(64, createRandomType);
        Test(pool, static pool => pool.Rent());
    }

    [Benchmark]
    public void Locking()
    {
        var pool = new LockingPool<RandomType>(64, createRandomType);
        Test(pool, static pool => pool.Rent());
    }
}
