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
    class Random
    {
        public int X { get; set; }
    }

    [Params(100, 1000)]
    public int N;

    public void Test<TPool, TLease>(TPool pool, Func<TPool, TLease> rent)
        where TLease : struct, IDisposable
    {
        for (int i = 0; i < N; i++)
        {
            using var lease = rent(pool);
        }
    }

    [Benchmark]
    public void ThreadStatic()
    {
        var pool = new ThreadStaticPool<Random>(() => new Random { X = 0 });
        Test(pool, static pool => pool.Rent());
    }

    //[Benchmark]
    //public void Spinning()
    //{
    //    var pool = new SpiningPool<Random>(64, () => new Random { X = 0 }); // Looks like it deadlocks 
    //    Parallel.For(0, 1000, _ =>
    //    {
    //        using var lease = pool.Rent();
    //        lease.Value.X += 1;
    //    });
    //}

    [Benchmark]
    public void Locking()
    {
        var pool = new LockingPool<Random>(64, () => new Random { X = 0 });
        Test(pool, static pool => pool.Rent());
    }
}
