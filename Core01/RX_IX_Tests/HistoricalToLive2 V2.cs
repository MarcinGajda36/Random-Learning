using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RXTests;
public static class HistoricalToLive2_V2
{
    private enum MessageType : byte
    {
        Live = 0,
        Historical,
        HistoricalError,
        HistoricalCompleted,
    }

    private readonly record struct Message<TValue>(MessageType Type, object? Value);

    private sealed class ConcatState<TValue>
    {
        private List<TValue>? liveBuffer = new();
        private bool hasHistoricalEnded;

        public IList<TValue> HandleNextMessage(Message<TValue> message)
            => message.Type switch // Maybe try if/else if; maybe if only for live? 
            {
                MessageType.Live => HandleLiveMessage((TValue)message.Value!),
                MessageType.Historical => (IList<TValue>)message.Value!,
                MessageType.HistoricalError => throw (Exception)message.Value!,
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

        private IList<TValue> HandleLiveMessage(TValue value) // Can i have a function for handling live, and when history ends i swap function to unbranched?
        {
            if (hasHistoricalEnded)
            {
                return new[] { value };
            }
            liveBuffer!.Add(value);
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
        => live.Select(live => new Message<TValue>(MessageType.Live, live));

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .ToList()
        .Materialize()
        .Select(notification => notification.Kind switch
        {
            NotificationKind.OnNext => new Message<TValue>(MessageType.Historical, notification.Value),
            NotificationKind.OnError => new Message<TValue>(MessageType.HistoricalError, notification.Exception),
            NotificationKind.OnCompleted => new Message<TValue>(MessageType.HistoricalCompleted, null),
            _ => throw new InvalidOperationException($"Unknown notification: '{notification}'."),
        });
}