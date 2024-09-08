using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;
internal class Primitives
{
    long x;
    public Task Test()
    {
        Thread.MemoryBarrier(); // This one delegates to Interlocked.MemoryBarrier();
        Interlocked.MemoryBarrier();
        var _x1 = Interlocked.Read(ref x); // Is this stronger then Volatile.Read(...)?
        var _x2 = Volatile.Read(ref x);
        return Task.CompletedTask;
    }
}
