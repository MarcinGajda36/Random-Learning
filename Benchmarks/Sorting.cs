using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using MarcinGajda.Collections;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
public class Sorting
{
    //[Params(2, 3, 4, 5, 6, 7, 8, 9, 10, 11)]
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
    public void LinqSort()
    {
        array.OrderBy(x => x).ToArray();
    }
}
// | Method | BidsInGroup | ArrayLength |         Mean |      Error |     StdDev |       Median | Rank | Allocated |
// |------- |------------ |------------ |-------------:|-----------:|-----------:|-------------:|-----:|----------:|
// |  Radix |           2 |           8 |     1.735 us |  0.0365 us |  0.0956 us |     1.700 us |    3 |   1.28 KB |
// |  Radix |           2 |          64 |     4.762 us |  0.0964 us |  0.1557 us |     4.800 us |   12 |   1.28 KB |
// |  Radix |           2 |         512 |    28.546 us |  0.4952 us |  0.4136 us |    28.500 us |   25 |   1.28 KB |
// |  Radix |           2 |        2048 |   108.403 us |  2.1491 us |  3.2166 us |   107.950 us |   33 |   1.28 KB |
// |  Radix |           2 |       32768 | 1,745.407 us | 17.7798 us | 16.6312 us | 1,741.700 us |   44 |   1.28 KB |
// |  Radix |           2 |       65536 | 3,418.264 us | 24.8919 us | 22.0660 us | 3,415.300 us |   50 |   1.28 KB |
// |  Radix |           3 |           8 |     1.608 us |  0.0399 us |  0.1145 us |     1.600 us |    2 |   1.28 KB |
// |  Radix |           3 |          64 |     3.757 us |  0.0789 us |  0.1647 us |     3.700 us |   10 |   1.28 KB |
// |  Radix |           3 |         512 |    20.317 us |  0.2933 us |  0.2290 us |    20.300 us |   24 |   1.28 KB |
// |  Radix |           3 |        2048 |    77.273 us |  1.2927 us |  1.2092 us |    76.800 us |   32 |   1.28 KB |
// |  Radix |           3 |       32768 | 1,200.320 us | 23.8396 us | 22.2996 us | 1,206.200 us |   42 |   1.28 KB |
// |  Radix |           3 |       65536 | 2,461.457 us | 48.9327 us | 45.7716 us | 2,447.150 us |   48 |   1.28 KB |
// |  Radix |           4 |           8 |     1.548 us |  0.0678 us |  0.1945 us |     1.500 us |    1 |   1.28 KB |
// |  Radix |           4 |          64 |     3.065 us |  0.0639 us |  0.1152 us |     3.050 us |    7 |   1.28 KB |
// |  Radix |           4 |         512 |    14.887 us |  0.2948 us |  0.2895 us |    15.000 us |   23 |   1.28 KB |
// |  Radix |           4 |        2048 |    56.821 us |  1.0265 us |  0.9099 us |    56.800 us |   31 |   1.28 KB |
// |  Radix |           4 |       32768 | 1,979.354 us | 21.1157 us | 17.6325 us | 1,983.700 us |   46 |   1.28 KB |
// |  Radix |           4 |       65536 | 4,239.550 us | 21.8382 us | 20.4275 us | 4,239.850 us |   52 |   1.28 KB |
// |  Radix |           5 |           8 |     1.765 us |  0.0544 us |  0.1569 us |     1.700 us |    3 |   1.28 KB |
// |  Radix |           5 |          64 |     3.022 us |  0.0624 us |  0.1329 us |     3.000 us |    6 |   1.28 KB |
// |  Radix |           5 |         512 |    13.431 us |  0.1854 us |  0.1548 us |    13.400 us |   21 |   1.28 KB |
// |  Radix |           5 |        2048 |    49.850 us |  0.7005 us |  0.5469 us |    49.800 us |   30 |   1.28 KB |
// |  Radix |           5 |       32768 | 1,839.779 us | 11.0318 us |  9.7794 us | 1,840.650 us |   45 |   1.28 KB |
// |  Radix |           5 |       65536 | 3,644.713 us | 22.5080 us | 21.0540 us | 3,640.700 us |   51 |   1.28 KB |
// |  Radix |           6 |           8 |     1.812 us |  0.0403 us |  0.1157 us |     1.800 us |    3 |   1.28 KB |
// |  Radix |           6 |          64 |     3.234 us |  0.0664 us |  0.1458 us |     3.200 us |    8 |   1.28 KB |
// |  Radix |           6 |         512 |    11.724 us |  0.0899 us |  0.1200 us |    11.700 us |   20 |   1.28 KB |
// |  Radix |           6 |        2048 |    40.608 us |  0.4649 us |  0.3630 us |    40.550 us |   29 |   1.28 KB |
// |  Radix |           6 |       32768 | 1,060.508 us | 15.8083 us | 13.2007 us | 1,063.600 us |   41 |   1.28 KB |
// |  Radix |           6 |       65536 | 3,011.560 us | 12.5384 us | 11.7285 us | 3,011.500 us |   49 |   1.28 KB |
// |  Radix |           7 |           8 |     2.074 us |  0.0457 us |  0.1320 us |     2.050 us |    4 |   1.28 KB |
// |  Radix |           7 |          64 |     2.933 us |  0.0589 us |  0.1047 us |     2.900 us |    6 |   1.28 KB |
// |  Radix |           7 |         512 |    10.329 us |  0.1560 us |  0.1383 us |    10.300 us |   18 |   1.28 KB |
// |  Radix |           7 |        2048 |    35.931 us |  0.2364 us |  0.1974 us |    35.800 us |   28 |   1.28 KB |
// |  Radix |           7 |       32768 |   770.936 us |  9.2012 us |  8.1566 us |   767.450 us |   39 |   1.28 KB |
// |  Radix |           7 |       65536 | 2,117.760 us | 14.9607 us | 13.9942 us | 2,114.600 us |   47 |   1.28 KB |
// |  Radix |           8 |           8 |     2.521 us |  0.0537 us |  0.1109 us |     2.500 us |    5 |   1.28 KB |
// |  Radix |           8 |          64 |     3.450 us |  0.0715 us |  0.0905 us |     3.450 us |    9 |   1.28 KB |
// |  Radix |           8 |         512 |     9.287 us |  0.1558 us |  0.1457 us |     9.200 us |   16 |   1.28 KB |
// |  Radix |           8 |        2048 |    29.694 us |  0.3635 us |  0.3733 us |    29.700 us |   26 |   1.28 KB |
// |  Radix |           8 |       32768 |   482.847 us |  7.1215 us |  6.6615 us |   484.400 us |   36 |   1.28 KB |
// |  Radix |           8 |       65536 | 1,053.580 us | 15.1433 us | 14.1651 us | 1,052.700 us |   41 |   1.28 KB |
// |  Radix |           9 |           8 |     4.079 us |  0.0820 us |  0.1436 us |     4.100 us |   11 |   1.28 KB |
// |  Radix |           9 |          64 |     4.873 us |  0.0978 us |  0.1661 us |     4.900 us |   13 |   1.28 KB |
// |  Radix |           9 |         512 |    10.614 us |  0.2069 us |  0.1834 us |    10.600 us |   19 |   1.28 KB |
// |  Radix |           9 |        2048 |    30.017 us |  0.2881 us |  0.2250 us |    30.000 us |   26 |   1.28 KB |
// |  Radix |           9 |       32768 |   606.860 us |  8.2655 us |  7.7315 us |   607.500 us |   37 |   1.28 KB |
// |  Radix |           9 |       65536 | 1,404.127 us | 10.0433 us |  9.3945 us | 1,406.000 us |   43 |   1.28 KB |
// |  Radix |          10 |           8 |     6.817 us |  0.1369 us |  0.1465 us |     6.800 us |   14 |   1.28 KB |
// |  Radix |          10 |          64 |     7.620 us |  0.1468 us |  0.1373 us |     7.600 us |   15 |   1.28 KB |
// |  Radix |          10 |         512 |    13.214 us |  0.2293 us |  0.2033 us |    13.200 us |   21 |   1.28 KB |
// |  Radix |          10 |        2048 |    32.675 us |  0.3709 us |  0.2896 us |    32.700 us |   27 |   1.28 KB |
// |  Radix |          10 |       32768 |   456.420 us |  6.6414 us |  6.2123 us |   454.700 us |   35 |   1.28 KB |
// |  Radix |          10 |       65536 |   948.930 us | 13.3215 us | 12.4609 us |   946.550 us |   40 |   1.28 KB |
// |  Radix |          11 |           8 |     9.329 us |  0.1738 us |  0.1541 us |     9.400 us |   16 |   1.28 KB |
// |  Radix |          11 |          64 |     9.804 us |  0.1976 us |  0.2638 us |     9.800 us |   17 |   1.28 KB |
// |  Radix |          11 |         512 |    14.379 us |  0.2549 us |  0.2259 us |    14.400 us |   22 |   1.28 KB |
// |  Radix |          11 |        2048 |    29.178 us |  0.5761 us |  0.7286 us |    28.800 us |   25 |   1.28 KB |
// |  Radix |          11 |       32768 |   346.143 us |  3.9639 us |  3.5139 us |   345.100 us |   34 |   1.28 KB |
// |  Radix |          11 |       65536 |   735.300 us |  3.4585 us |  3.0659 us |   736.500 us |   38 |   1.28 KB |

// Radix bidsInGroup = 8
// | Method | ArrayLength |         Mean |     Error |    StdDev |       Median | Rank | Allocated |
// |------- |------------ |-------------:|----------:|----------:|-------------:|-----:|----------:|
// |  Radix |           8 |     2.565 us | 0.0597 us | 0.1712 us |     2.500 us |    1 |   1.28 KB |
// |  Radix |          64 |     3.544 us | 0.0736 us | 0.1124 us |     3.550 us |    2 |   1.28 KB |
// |  Radix |         512 |     9.293 us | 0.1754 us | 0.1555 us |     9.300 us |    3 |   1.28 KB |
// |  Radix |        2048 |    30.077 us | 0.5584 us | 0.8527 us |    29.800 us |    4 |   1.28 KB |
// |  Radix |       32768 |   490.920 us | 3.8425 us | 3.5943 us |   491.200 us |    5 |   1.28 KB |
// |  Radix |       65536 | 1,071.663 us | 8.6934 us | 8.1318 us | 1,071.250 us |    6 |   1.28 KB |

// Radix bidsInGroup = 8
// |    Method | ArrayLength |           Mean |         Error |        StdDev |         Median | Rank | Allocated |
// |---------- |------------ |---------------:|--------------:|--------------:|---------------:|-----:|----------:|
// |     Radix |           8 |     2,702.2 ns |      55.62 ns |     156.88 ns |     2,700.0 ns |    4 |    1312 B |
// | ArraySort |           8 |       450.0 ns |       0.00 ns |       0.00 ns |       450.0 ns |    1 |     640 B |
// |  LinqSort |           8 |     2,048.9 ns |      53.11 ns |     151.51 ns |     2,000.0 ns |    3 |    1040 B |
// |     Radix |          64 |     3,521.6 ns |      78.54 ns |     227.86 ns |     3,500.0 ns |    5 |    1312 B |
// | ArraySort |          64 |     1,422.2 ns |      37.17 ns |     103.62 ns |     1,400.0 ns |    2 |     640 B |
// |  LinqSort |          64 |     5,750.0 ns |     117.89 ns |     206.47 ns |     5,750.0 ns |    6 |    1936 B |
// |     Radix |         512 |     9,403.8 ns |     186.52 ns |     255.31 ns |     9,350.0 ns |    7 |    1312 B |
// | ArraySort |         512 |    13,826.7 ns |     163.97 ns |     153.37 ns |    13,800.0 ns |    8 |     640 B |
// |  LinqSort |         512 |    46,566.7 ns |     458.03 ns |     357.60 ns |    46,650.0 ns |   10 |    9104 B |
// |     Radix |        2048 |    29,164.3 ns |     439.37 ns |     389.49 ns |    29,150.0 ns |    9 |    1312 B |
// | ArraySort |        2048 |    66,240.0 ns |   1,301.70 ns |   1,217.61 ns |    65,900.0 ns |   11 |     640 B |
// |  LinqSort |        2048 |   225,926.7 ns |   2,403.93 ns |   2,248.64 ns |   225,200.0 ns |   12 |   33680 B |
// |     Radix |       32768 |   489,164.3 ns |   4,666.18 ns |   4,136.44 ns |   489,800.0 ns |   13 |    1312 B |
// | ArraySort |       32768 | 1,383,800.0 ns |   4,111.36 ns |   3,433.17 ns | 1,384,100.0 ns |   15 |     640 B |
// |  LinqSort |       32768 | 5,045,300.0 ns |  17,621.77 ns |  16,483.41 ns | 5,047,500.0 ns |   17 |  525200 B |
// |     Radix |       65536 | 1,043,771.4 ns |  11,897.34 ns |  10,546.69 ns | 1,044,750.0 ns |   14 |    1312 B |
// | ArraySort |       65536 | 2,965,730.8 ns |   6,054.66 ns |   5,055.92 ns | 2,966,600.0 ns |   16 |     640 B |
// |  LinqSort |       65536 | 8,684,971.8 ns | 101,125.12 ns | 177,112.49 ns | 8,668,600.0 ns |   18 | 1049488 B |