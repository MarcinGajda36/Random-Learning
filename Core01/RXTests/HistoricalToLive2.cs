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

    internal readonly record struct Message<TValue>(MessageType Type, TValue? Value, Exception? Exception)
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
        => live.Select(live => Message<TValue>.Live(live));

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .Materialize()
        .Select(notification =>
        {
            if (notification.Kind is NotificationKind.OnNext)
            {
                return Message<TValue>.Historical(notification.Value);
            }

            if (notification.Kind is NotificationKind.OnCompleted)
            {
                return Message<TValue>.HistoricalCompleted();
            }

            if (notification.Kind is NotificationKind.OnError)
            {
                return Message<TValue>.HistoricalError(notification.Exception);
            }

            throw new InvalidOperationException($"Unknown notification: '{notification}'.");
        });
}