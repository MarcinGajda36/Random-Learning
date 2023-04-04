using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RX_IX_Tests;
public class HistoricalToLive3
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

    readonly record struct ConcatState<TValue>(List<TValue>? LiveBuffer, bool HasHistoricalEnded, IList<TValue> AvailableReturn);

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(new ConcatState<TValue>(new List<TValue>(), false, Array.Empty<TValue>()), HandleNextMessage)
        .SelectMany(state => state.AvailableReturn);

    private static ConcatState<TValue> HandleNextMessage<TValue>(ConcatState<TValue> state, Message<TValue> message)
    {
        if (state.HasHistoricalEnded)
        {
            return state with { AvailableReturn = message.Values };
        }

        return message.Type switch
        {
            MessageType.Live => HandleLiveDuringHistory(state, message.Values),
            MessageType.Historical => state with { AvailableReturn = message.Values },
            MessageType.HistoricalError => throw message.Exception!,
            MessageType.HistoricalCompleted => state with { AvailableReturn = state.LiveBuffer!, HasHistoricalEnded = true, LiveBuffer = null },
            _ => throw new InvalidOperationException($"Unknown message: '{message}'."),
        };
    }

    private static ConcatState<TValue> HandleLiveDuringHistory<TValue>(in ConcatState<TValue> state, IList<TValue> values)
    {
        state.LiveBuffer!.Add(values[0]);
        return state with { AvailableReturn = Array.Empty<TValue>() };
    }

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