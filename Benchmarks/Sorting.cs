using BenchmarkDotNet.Attributes;
using MarcinGajda.Collections;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class Sorting
{
    //[Params(2, 4, 8)]
    //public int BidsInGroup { get; set; }

    [Params(8, 64, 512, 2_048, 32_768, 65_536)]
    public int ArrayLength { get; set; }

    private uint[] array;

    [IterationSetup]
    public void Setup()
    {
        array = new uint[ArrayLength];
        for (uint i = 0; i < array.Length; i++)
        {
            unchecked
            {
                array[i] = i * 2654435769u;
            }
        }
    }

    [Benchmark]
    public void Radix()
    {
        array.RadixSort(8);
    }

    [Benchmark]
    public void ArraySort()
    {
        Array.Sort(array);
    }

    [Benchmark]
    public void SpanSort()
    {
        array.AsSpan().Sort();
    }

    [Benchmark]
    public void LinqSort()
    {
        array.OrderBy(x => x).ToArray();
    }
}

// | Method | BidsInGroup | ArrayLength |         Mean |      Error |      StdDev |       Median | Rank | Allocated |
// |------- |------------ |------------ |-------------:|-----------:|------------:|-------------:|-----:|----------:|
// |  Radix |           2 |           8 |     1.180 us |  0.0533 us |   0.1545 us |     1.100 us |    2 |     976 B |
// |  Radix |           2 |          64 |     4.273 us |  0.0864 us |   0.1784 us |     4.300 us |    6 |     976 B |
// |  Radix |           2 |         512 |    27.927 us |  0.3249 us |   0.2713 us |    27.950 us |    9 |     976 B |
// |  Radix |           2 |        2048 |   110.034 us |  2.1459 us |   4.6193 us |   108.850 us |   12 |     976 B |
// |  Radix |           2 |       32768 | 1,796.029 us | 22.5738 us |  20.0111 us | 1,794.800 us |   15 |     976 B |
// |  Radix |           2 |       65536 | 3,738.455 us | 74.1316 us | 139.2372 us | 3,672.900 us |   17 |     976 B |
// |  Radix |           4 |           8 |     1.045 us |  0.0249 us |   0.0691 us |     1.000 us |    1 |     976 B |
// |  Radix |           4 |          64 |     2.709 us |  0.0554 us |   0.1080 us |     2.700 us |    4 |     976 B |
// |  Radix |           4 |         512 |    14.595 us |  0.2805 us |   0.3118 us |    14.500 us |    8 |     976 B |
// |  Radix |           4 |        2048 |    55.186 us |  0.5416 us |   0.4802 us |    55.250 us |   11 |     976 B |
// |  Radix |           4 |       32768 | 1,863.485 us | 17.1383 us |  14.3112 us | 1,863.200 us |   16 |     976 B |
// |  Radix |           4 |       65536 | 4,145.811 us | 79.3617 us | 113.8181 us | 4,083.100 us |   18 |     976 B |
// |  Radix |           8 |           8 |     2.134 us |  0.0441 us |   0.0725 us |     2.100 us |    3 |     976 B |
// |  Radix |           8 |          64 |     2.871 us |  0.0596 us |   0.0854 us |     2.900 us |    5 |     976 B |
// |  Radix |           8 |         512 |     8.836 us |  0.1751 us |   0.2150 us |     8.800 us |    7 |     976 B |
// |  Radix |           8 |        2048 |    29.323 us |  0.4941 us |   0.4126 us |    29.300 us |   10 |     976 B |
// |  Radix |           8 |       32768 |   568.873 us |  7.2220 us |   6.7555 us |   565.200 us |   13 |     976 B |
// |  Radix |           8 |       65536 | 1,204.450 us |  5.0285 us |   3.9259 us | 1,205.000 us |   14 |     976 B |

// |    Method | ArrayLength |           Mean |        Error |        StdDev |         Median | Rank | Allocated |
// |---------- |------------ |---------------:|-------------:|--------------:|---------------:|-----:|----------:|
// | ArraySort |           8 |       434.4 ns |     24.55 ns |      70.83 ns |       400.0 ns |    1 |     640 B |
// |  SpanSort |           8 |       476.8 ns |     20.62 ns |      59.17 ns |       500.0 ns |    1 |     640 B |
// |  LinqSort |           8 |     1,915.4 ns |     42.26 ns |      98.79 ns |     1,900.0 ns |    4 |    1040 B |
// | ArraySort |          64 |     1,396.6 ns |     35.44 ns |      97.61 ns |     1,400.0 ns |    2 |     640 B |
// |  SpanSort |          64 |     1,513.3 ns |     47.89 ns |     139.69 ns |     1,500.0 ns |    3 |     640 B |
// |  LinqSort |          64 |     5,739.2 ns |    110.71 ns |     277.74 ns |     5,700.0 ns |    5 |    1936 B |
// | ArraySort |         512 |    13,933.3 ns |    251.26 ns |     235.03 ns |    13,900.0 ns |    6 |     640 B |
// |  SpanSort |         512 |    13,930.8 ns |    178.86 ns |     149.36 ns |    13,900.0 ns |    6 |     640 B |
// |  LinqSort |         512 |    46,907.1 ns |    634.23 ns |     562.23 ns |    46,850.0 ns |    7 |    9104 B |
// | ArraySort |        2048 |    65,850.0 ns |    361.50 ns |     320.46 ns |    65,850.0 ns |    8 |     640 B |
// |  SpanSort |        2048 |    66,153.3 ns |  1,103.53 ns |   1,032.24 ns |    65,900.0 ns |    8 |     640 B |
// |  LinqSort |        2048 |   223,792.9 ns |  1,579.58 ns |   1,400.26 ns |   223,500.0 ns |    9 |   33680 B |
// | ArraySort |       32768 | 1,391,576.9 ns |  4,907.76 ns |   4,098.20 ns | 1,390,800.0 ns |   10 |     640 B |
// |  SpanSort |       32768 | 1,384,746.2 ns |  4,769.31 ns |   3,982.59 ns | 1,385,400.0 ns |   10 |     640 B |
// |  LinqSort |       32768 | 5,073,173.3 ns | 30,544.03 ns |  28,570.90 ns | 5,070,100.0 ns |   12 |  525200 B |
// | ArraySort |       65536 | 2,983,820.0 ns |  7,893.40 ns |   7,383.50 ns | 2,982,200.0 ns |   11 |     640 B |
// |  SpanSort |       65536 | 2,972,792.9 ns |  8,901.60 ns |   7,891.03 ns | 2,973,150.0 ns |   11 |     640 B |
// |  LinqSort |       65536 | 8,946,366.2 ns | 82,938.58 ns | 140,836.01 ns | 8,911,550.0 ns |   13 | 1049488 B |

// bidsInGroup = 8
// |    Method | ArrayLength |           Mean |        Error |       StdDev |         Median | Rank | Allocated |
// |---------- |------------ |---------------:|-------------:|-------------:|---------------:|-----:|----------:|
// |     Radix |           8 |     2,109.3 ns |     43.46 ns |     91.67 ns |     2,100.0 ns |    6 |     976 B |
// | ArraySort |           8 |       452.6 ns |     25.90 ns |     75.13 ns |       400.0 ns |    1 |     640 B |
// |  SpanSort |           8 |       518.4 ns |     28.00 ns |     81.67 ns |       500.0 ns |    2 |     640 B |
// |  LinqSort |           8 |     2,026.7 ns |     50.39 ns |    140.46 ns |     2,000.0 ns |    5 |    1040 B |
// |     Radix |          64 |     2,948.3 ns |     59.42 ns |     87.10 ns |     3,000.0 ns |    7 |     976 B |
// | ArraySort |          64 |     1,442.9 ns |     52.15 ns |    152.13 ns |     1,450.0 ns |    3 |     640 B |
// |  SpanSort |          64 |     1,511.1 ns |     49.17 ns |    144.20 ns |     1,500.0 ns |    4 |     640 B |
// |  LinqSort |          64 |     5,890.6 ns |    118.05 ns |    183.79 ns |     5,900.0 ns |    8 |    1936 B |
// |     Radix |         512 |     8,715.4 ns |    153.41 ns |    128.10 ns |     8,700.0 ns |    9 |     976 B |
// | ArraySort |         512 |    13,864.3 ns |    185.66 ns |    164.58 ns |    13,800.0 ns |   10 |     640 B |
// |  SpanSort |         512 |    14,160.0 ns |    238.37 ns |    222.97 ns |    14,200.0 ns |   11 |     640 B |
// |  LinqSort |         512 |    46,784.6 ns |    411.50 ns |    343.62 ns |    46,700.0 ns |   13 |    9104 B |
// |     Radix |        2048 |    29,473.3 ns |    588.98 ns |    550.93 ns |    29,500.0 ns |   12 |     976 B |
// | ArraySort |        2048 |    65,884.6 ns |  1,194.39 ns |    997.37 ns |    65,600.0 ns |   14 |     640 B |
// |  SpanSort |        2048 |    65,978.6 ns |    742.27 ns |    658.00 ns |    65,650.0 ns |   14 |     640 B |
// |  LinqSort |        2048 |   225,026.7 ns |  1,630.08 ns |  1,524.78 ns |   224,600.0 ns |   15 |   33680 B |
// |     Radix |       32768 |   552,378.6 ns |  3,097.93 ns |  2,746.24 ns |   552,200.0 ns |   16 |     976 B |
// | ArraySort |       32768 | 1,385,107.1 ns |  1,655.23 ns |  1,467.32 ns | 1,385,050.0 ns |   18 |     640 B |
// |  SpanSort |       32768 | 1,383,915.4 ns |  3,187.14 ns |  2,661.41 ns | 1,383,500.0 ns |   18 |     640 B |
// |  LinqSort |       32768 | 5,068,892.9 ns | 32,602.55 ns | 28,901.31 ns | 5,069,850.0 ns |   20 |  525200 B |
// |     Radix |       65536 | 1,208,535.7 ns |  4,507.18 ns |  3,995.50 ns | 1,208,400.0 ns |   17 |     976 B |
// | ArraySort |       65536 | 2,971,292.9 ns |  9,106.52 ns |  8,072.70 ns | 2,969,200.0 ns |   19 |     640 B |
// |  SpanSort |       65536 | 2,964,161.5 ns | 10,893.94 ns |  9,096.94 ns | 2,961,800.0 ns |   19 |     640 B |
// |  LinqSort |       65536 | 8,930,757.5 ns | 24,603.17 ns | 43,732.11 ns | 8,929,900.0 ns |   21 | 1049488 B |