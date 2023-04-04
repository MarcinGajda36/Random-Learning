using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.RX_IX_Tests;
public static class HistoricalToLive3
{
    private enum MessageType : byte
    {
        Live = 0,
        Historical,
        HistoricalError,
        HistoricalCompleted,
    }

    private readonly record struct Message<TValue>(MessageType Type, IList<TValue> Values, Exception? Exception);

    private static class Message
    {
        public static Message<TValue> Live<TValue>(IList<TValue> value)
            => new(MessageType.Live, value, null);

        public static Message<TValue> Historical<TValue>(IList<TValue> value)
            => new(MessageType.Historical, value, null);

        public static Message<TValue> HistoricalError<TValue>(Exception exception)
            => new(MessageType.HistoricalError, Array.Empty<TValue>(), exception);

        public static Message<TValue> HistoricalCompleted<TValue>()
            => new(MessageType.HistoricalCompleted, Array.Empty<TValue>(), null);
    }

    private sealed class ConcatState<TValue>
    {
        private List<TValue>? liveBuffer = new();
        private bool hasHistoricalEnded;

        public IList<TValue> HandleNextMessage(Message<TValue> message)
            => message.Type switch
            {
                MessageType.Live => HandleLiveMessage(message.Values),
                MessageType.Historical => message.Values,
                MessageType.HistoricalError => throw message.Exception!,
                MessageType.HistoricalCompleted => HandleHistoricalCompletion(),
                _ => throw new InvalidOperationException($"Unknown message: '{message}'."),
            };

        private List<TValue> HandleHistoricalCompletion()
        {
            hasHistoricalEnded = true;
            var buffered = liveBuffer;
            liveBuffer = null;
            return buffered!;
        }

        private IList<TValue> HandleLiveMessage(IList<TValue> values)
        {
            if (hasHistoricalEnded)
            {
                return values;
            }
            liveBuffer!.Add(values[0]); // AddRange is so much less error-prone
            return Array.Empty<TValue>();
        }
    }

    private readonly record struct Concat<TValue>(IList<TValue> Return, ConcatState<TValue> State);

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

    private static Concat<TValue> HandleNextMessage<TValue>(Concat<TValue> previous, Message<TValue> message)
        => previous with { Return = previous.State.HandleNextMessage(message) };

    private static IObservable<Message<TValue>> GetLiveMessages<TValue>(IObservable<TValue> live)
        => live.Select(live => Message.Live(new[] { live }));

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .ToList()
        .Materialize()
        .Select(notification => notification.Kind switch
        {
            NotificationKind.OnNext => Message.Historical(notification.Value),
            NotificationKind.OnError => Message.HistoricalError<TValue>(notification.Exception!),
            NotificationKind.OnCompleted => Message.HistoricalCompleted<TValue>(),
            _ => throw new InvalidOperationException($"Unknown notification: '{notification}'."),
        });
}