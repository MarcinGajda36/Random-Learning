using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RXTests;
public static class HistoricalToLive2
{
    public enum MessageType : byte
    {
        Live = 0,
        Historical,
        HistoricalError,
        HistoricalCompleted,
    }

    internal readonly record struct Message<TValue>(MessageType Type, IList<TValue> Value, Exception? Exception)
    {
        public static Message<TValue> Live(TValue value)
            => new(MessageType.Live, new[] { value }, null);

        public static Message<TValue> Historical(IList<TValue> value)
            => new(MessageType.Historical, value, null);

        public static Message<TValue> HistoricalError(Exception exception)
            => new(MessageType.HistoricalError, Array.Empty<TValue>(), exception);

        public static Message<TValue> HistoricalCompleted()
            => new(MessageType.HistoricalCompleted, Array.Empty<TValue>(), null);
    }

    internal sealed class ConcatState<TValue>
    {
        private List<TValue>? liveBuffer;
        private bool hasHistoricalEnded;

        public IEnumerable<TValue> HandleNextMessage(in Message<TValue> message)
        {
            if (message.Type is MessageType.Live)
            {
                if (hasHistoricalEnded)
                {
                    return message.Value;
                }

                liveBuffer ??= new List<TValue>();
                liveBuffer.Add(message.Value[0]);
                return Enumerable.Empty<TValue>();
            }

            if (message.Type is MessageType.Historical)
            {
                return message.Value;
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

    internal readonly record struct Concat<TValue>(IEnumerable<TValue> Return, ConcatState<TValue> State);

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(
            new Concat<TValue>(Enumerable.Empty<TValue>(), new ConcatState<TValue>()),
            (state, message) => HandleNextMessage(state, in message))
        .SelectMany(state => state.Return);

    internal static Concat<TValue> HandleNextMessage<TValue>(Concat<TValue> previous, in Message<TValue> message)
        => previous with { Return = previous.State.HandleNextMessage(in message) };

    private static IObservable<Message<TValue>> GetLiveMessages<TValue>(IObservable<TValue> live)
        => live.Select(Message<TValue>.Live);

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .ToList()
        .Materialize()
        .Select(notification => notification.Kind switch
        {
            NotificationKind.OnNext => Message<TValue>.Historical(notification.Value),
            NotificationKind.OnError => Message<TValue>.HistoricalError(notification.Exception),
            NotificationKind.OnCompleted => Message<TValue>.HistoricalCompleted(),
            _ => throw new InvalidOperationException($"Unknown notification: '{notification}'."),
        });
}