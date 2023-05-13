using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RX_IX_Tests;
public static class HistoricalToLive_IList
{
    private enum MessageType : byte
    {
        Live = 0,
        Historical,
        HistoricalError,
        HistoricalCompleted,
    }

    private readonly record struct Message<TValue>(MessageType Type, IList<TValue> Values, Exception? Exception);

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
            liveBuffer!.AddRange(values);
            return Array.Empty<TValue>();
        }
    }

    private readonly record struct Concat<TValue>(IList<TValue> Return, ConcatState<TValue> State);

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(
            new Concat<TValue>(Array.Empty<TValue>(), new ConcatState<TValue>()),
            HandleNextMessage)
        .SelectMany(state => state.Return);

    private static Concat<TValue> HandleNextMessage<TValue>(Concat<TValue> previous, Message<TValue> message)
        => previous with { Return = previous.State.HandleNextMessage(message) };

    private static IObservable<Message<TValue>> GetLiveMessages<TValue>(IObservable<TValue> live)
        => live
        .Buffer(TimeSpan.FromMilliseconds(16d), 16)
        .Select(live => new Message<TValue>(MessageType.Live, live, null));

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .ToList()
        .Materialize()
        .Select(notification => notification.Kind switch
        {
            NotificationKind.OnNext => new Message<TValue>(MessageType.Historical, notification.Value, null),
            NotificationKind.OnError => new Message<TValue>(MessageType.HistoricalError, Array.Empty<TValue>(), notification.Exception!),
            NotificationKind.OnCompleted => new Message<TValue>(MessageType.HistoricalCompleted, Array.Empty<TValue>(), null),
            _ => throw new InvalidOperationException($"Unknown notification: '{notification}'."),
        });
}