using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
public class Concats
{
    private readonly int[] elements = [1, 2, 3];

    [Params(10_000)]
    public int Iterations { get; set; }

    private IEnumerable<int> Accumulate(IEnumerable<int> first)
    {
        for (int i = 0; i < Iterations; i++)
        {
            first = first.Concat(elements);
        }
        return first;
    }

    [Benchmark]
    public void StartsAsEnumerableEmpty_ToList()
    {
        var accumulator = Enumerable.Empty<int>();
        Accumulate(accumulator).ToList();
    }

    [Benchmark]
    public void StartsAsArrayEmpty_ToList()
    {
        IEnumerable<int> accumulator = Array.Empty<int>();
        Accumulate(accumulator).ToList();
    }

    [Benchmark]
    public void ListAddRange()
    {
        var accumulator = new List<int>();
        for (int i = 0; i < Iterations; i++)
        {
            accumulator.AddRange(elements);
        }
    }

    [Benchmark]
    public void StartsAsEnumerableEmpty_ToArray()
    {
        var accumulator = Enumerable.Empty<int>();
        Accumulate(accumulator).ToArray();
    }

    [Benchmark]
    public void StartsAsArrayEmpty_ToArray()
    {
        IEnumerable<int> accumulator = Array.Empty<int>();
        Accumulate(accumulator).ToArray();
    }

    [Benchmark]
    public void ListAddRange_ToArray()
    {
        var accumulator = new List<int>();
        for (int i = 0; i < Iterations; i++)
        {
            accumulator.AddRange(elements);
        }
        accumulator.ToArray();
    }
}
