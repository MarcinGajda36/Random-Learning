using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.RX_IX_Tests;
public static class HistoricalToLive3
{
    // System.Interactive.Async fails to get benchmarked https://github.com/dotnet/reactive/blob/main/Ix.NET/Source/System.Interactive.Async/System/Linq/Operators/Merge.cs
    public static async IAsyncEnumerable<TValue> ConcatLiveAfterHistory<TValue>(
        IAsyncEnumerable<TValue> live,
        IAsyncEnumerable<TValue> historical,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        bool hasHistoricalEnded = false;
        var liveBuffer = new List<TValue>();
        await using var liveEnumerator = live.GetAsyncEnumerator(cancellationToken);

        async Task BufferLiveUntilHistoryEnds()
        {
            while (hasHistoricalEnded != true
                && await liveEnumerator.MoveNextAsync(cancellationToken))
            {
                liveBuffer.Add(liveEnumerator.Current);
            }
        }

        var liveBufferTask = BufferLiveUntilHistoryEnds();

        await foreach (var history in historical)
        {
            yield return history;
        }
        hasHistoricalEnded = true;
        await liveBufferTask;

        foreach (var buffered in liveBuffer)
        {
            yield return buffered;
        }

        while (await liveEnumerator.MoveNextAsync(cancellationToken))
        {
            yield return liveEnumerator.Current;
        }
    }
}