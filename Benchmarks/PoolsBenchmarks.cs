using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using MarcinGajda.Synchronizers.Pooling;

namespace Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.CacheMisses)]
public class PoolsBenchmarks
{
    class Random
    {
        public int X { get; set; }
    }

    [Benchmark]
    public void ThreadStatic()
    {
        var pool = new ThreadStaticPool<Random>(() => new Random { X = 0 });
        Parallel.For(0, 1000, _ =>
        {
            using var lease = pool.Rent();
            lease.Value.X += 1;
        });
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
        Parallel.For(0, 1000, _ =>
        {
            using var lease = pool.Rent();
            lease.Value.X += 1;
        });
    }
}
