using System;
using System.Buffers;

namespace SpansAndStuff
{
    class Program
    {
        static void Main()
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
                DisplayBufferToConsole(memory.Slice(0, value.ToString().Length));
            }
            catch (FormatException)
            {
                Console.WriteLine("You did not enter a valid number.");
            }
            catch (OverflowException)
            {
                Console.WriteLine($"You entered a number less than {int.MinValue:N0} or greater than {int.MaxValue:N0}.");
            }
        }

        static void WriteInt32ToBuffer(int value, Memory<char> buffer)
        {
            string strValue = value.ToString();

            Span<char> span = buffer.Slice(0, strValue.Length).Span;
            strValue.AsSpan().CopyTo(span);
        }

        static void DisplayBufferToConsole(Memory<char> buffer) =>
            Console.WriteLine($"Contents of the buffer: '{buffer}'");

        static void Test()
        {
            string str = "Marcin";
            ReadOnlyMemory<char> mem = str.AsMemory();
            ReadOnlySpan<char> spn = str.AsSpan();
            Memory<char> dest = new char[64];
            str.AsSpan().CopyTo(dest.Slice(0, str.Length).Span);

        }

    }
}
