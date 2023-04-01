using System;
using System.Threading.Tasks;

namespace MarcinGajda.AsyncDispose_
{
    public class AsyncDisposableTests
    {
        public static async Task Test()
        {
            await using (var a = new AsyncDisposableObject())
            {

            }
            await Task.Delay(100);
        }

    }
    public class AsyncDisposableObject : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Console.WriteLine(1);
            await Task.Delay(100);
            Console.WriteLine(2);
            await Task.Delay(100);
            Console.WriteLine(3);
            await Task.Delay(100);
        }
    }
}
