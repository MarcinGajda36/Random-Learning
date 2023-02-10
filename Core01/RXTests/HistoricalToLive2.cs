using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RXTests;
internal static class HistoricalToLive2
{
    public enum MessageType : byte
    {
        Live = 0,
        Historical,
        HistoricalError,
        HistoricalCompleted,
    }

    private readonly record struct Message<TValue>(MessageType Type, TValue? Value, Exception? Exception)
    {
        public static Message<TValue> Live(TValue value)
            => new(MessageType.Live, value, null);

        public static Message<TValue> Historical(TValue value)
            => new(MessageType.Historical, value, null);

        public static Message<TValue> HistoricalError(Exception exception)
            => new(MessageType.HistoricalError, default, exception);

        public static Message<TValue> HistoricalCompleted()
            => new(MessageType.HistoricalCompleted, default, null);
    }

    private sealed class ConcatState<TValue>
    {
        // Mutable List and LinkedList don't use IEqualityComparer, is there order preserving mutable collection that takes IEqualityComparer?
        // private readonly IEqualityComparer<TValue>? comparer;  
        private List<TValue>? liveBuffer;
        private bool hasHistoricalEnded;

        public IEnumerable<TValue> HandleNextMessage(in Message<TValue> message)
        {
            if (message.Type is MessageType.Live)
            {
                if (hasHistoricalEnded)
                {
                    return new[] { message.Value! };
                }

                if (liveBuffer is null)
                {
                    liveBuffer = new List<TValue>();
                }
                liveBuffer.Add(message.Value!);
                return Enumerable.Empty<TValue>();
            }

            if (message.Type is MessageType.Historical)
            {
                if (liveBuffer is not null)
                {
                    _ = liveBuffer.Remove(message.Value!);
                }
                return new[] { message.Value! };
            }

            if (message.Type is MessageType.HistoricalCompleted)
            {
                hasHistoricalEnded = true;
                if (liveBuffer is null)
                {
                    return Enumerable.Empty<TValue>();
                }
                var buffered = liveBuffer;
                liveBuffer = null;
                return buffered;
            }

            if (message.Type is MessageType.HistoricalError)
            {
                throw message.Exception!;
            }

            throw new InvalidOperationException($"Unknown message: '{message}'.");
        }
    }

    private readonly record struct Concat<TValue>(IEnumerable<TValue> Return, ConcatState<TValue> State);

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(
            new Concat<TValue>(Enumerable.Empty<TValue>(), new ConcatState<TValue>()),
            (state, message) => HandleNextMessage(in state, in message))
        .SelectMany(state => state.Return);

    private static Concat<TValue> HandleNextMessage<TValue>(in Concat<TValue> previous, in Message<TValue> message)
        => new(previous.State.HandleNextMessage(in message), previous.State); // or return previous with { Return = previous.State.HandleNextMessage(in message) }

    private static IObservable<Message<TValue>> GetLiveMessages<TValue>(IObservable<TValue> live)
        => live.Select(live => Message<TValue>.Live(live));

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .Materialize()
        .Select(notification => notification switch
        {
            { Kind: NotificationKind.OnNext, Value: var value } => Message<TValue>.Historical(value),
            { Kind: NotificationKind.OnError, Exception: var exception } => Message<TValue>.HistoricalError(exception),
            { Kind: NotificationKind.OnCompleted } => Message<TValue>.HistoricalCompleted(),
            var unknown => throw new InvalidOperationException($"Unknown notification: '{unknown}'.")
        });
}