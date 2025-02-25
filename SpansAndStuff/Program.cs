using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PerKeySynchronizers.BoundedParallelism;
using PerKeySynchronizers.UnboundedParallelism;

namespace SpansAndStuff;

internal class Program
{
    private static async Task Main()
    {
        Vectors.EqualsAny();
        ;
        await Synchronization();
        Net8Tests();
        RefTestArray();

        AsyncTaskMethodBuilder<int> asyncTaskMethodBuilder = default;

        await Task.Delay(-1);
    }

    public static async Task Synchronization()
    {
        var perKey = new PerKeySynchronizer();
        var one = 1;
        var two = await perKey.SynchronizeAsync(1, async token => one += 1);
        await perKey.SynchronizeAllAsync(async token => one += 1);
        ;
        var perKeyGuid = new PerKeySynchronizer<Guid>();
        var four = await perKeyGuid.SynchronizeAsync(Guid.Empty, async token => one += 1);

    }

    private static void Switches()
    {
        var array = new[] { 1, 2, 3 };
        var r = array.AsSpan() switch
        {
            [] => 0,
            [var a] => 1,
            [var a, .. var b] => 1 + b.Length,
        };
        HashSet<int> set = [123, 234, 345];
    }

    private static void Net8Tests()
    {
        var format = CompositeFormat.Parse("Composite {0}");
        var formatted = string.Format(CultureInfo.InvariantCulture, format, "Text");

        var searchValue = SearchValues.Create([2, 3]);
        var found = (new byte[] { 1, 2, 3 }).AsSpan().IndexOfAny(searchValue);
    }

    private static void RefTestDict()
    {
        var d = new Dictionary<int, int>();
        ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(d, 1);
        if (Unsafe.IsNullRef(ref value) is false)
        {
            // Here i can use value
            return;
        }
    }

    private static int RefTestArray()
    {
        var arr = new[] { 1, 2, 3 };
        ref var first = ref arr[0];
        var firstPlus2 = first + 2;
        first = 0;
        var newFirst = arr[0];
        return 0;
    }

    private static void Tests()
    {
        ArraySegments.Test4();
        Test();

        using IMemoryOwner<char> owner = MemoryPool<char>.Shared.Rent();
        Console.Write("Enter a number: ");
        try
        {
            int value = int.Parse(Console.ReadLine());
            Memory<char> memory = owner.Memory;
            WriteInt32ToBuffer(value, memory);
            DisplayBufferToConsole(memory[..value.ToString().Length]);
            var list = new List<int>();
            var listSpan = CollectionsMarshal.AsSpan(list);
        }
        catch (FormatException)
        {
            Console.WriteLine("You did not enter a valid number.");
        }
        catch (OverflowException)
        {
            Console.WriteLine($"You entered a number less than {int.MinValue:N0} or greater than {int.MaxValue:N0}.");
        }
        var test = new Test();
        ITest test1 = test;
        _ = test1.Name;
        test1.Method();
    }

    private static void WriteInt32ToBuffer(int value, Memory<char> buffer)
    {
        string strValue = value.ToString();

        Span<char> span = buffer.Slice(0, strValue.Length).Span;
        strValue.AsSpan().CopyTo(span);
    }

    private static void DisplayBufferToConsole(Memory<char> buffer) =>
        Console.WriteLine($"Contents of the buffer: '{buffer}'");

    private static void Test()
    {
        string str = "Marcin";
        ReadOnlyMemory<char> mem = str.AsMemory();
        ReadOnlySpan<char> spn = str.AsSpan();
        Memory<char> dest = new char[64];
        str.AsSpan().CopyTo(dest.Slice(0, str.Length).Span);

    }
}
