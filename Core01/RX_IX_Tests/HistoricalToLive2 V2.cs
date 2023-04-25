using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RX_IX_Tests;
public static class HistoricalToLive2_V2
{
    private enum MessageType : byte
    {
        Live = 0,
        Historical,
        HistoricalError,
        HistoricalCompleted,
    }

    private readonly record struct Message(MessageType Type, object? Value);

    //interface IHandler<TValue> { IList<TValue> Handle(Message message); } // TODO can try more OOP
    private sealed class ConcatState<TValue>
    {
        public Func<Message, IList<TValue>> Handler { get; private set; }

        public ConcatState()
        {
            Handler = HistoryAndLiveHandler();
        }

        private static IList<TValue> LiveHandler(Message message)
            => new[] { (TValue)message.Value! };

        private Func<Message, IList<TValue>> HistoryAndLiveHandler()
        {
            List<TValue> liveBuffer = new();
            return (message) => message.Type switch
            {
                MessageType.Live => HandleLiveMessage(liveBuffer, (TValue)message.Value!),
                MessageType.Historical => (IList<TValue>)message.Value!,
                MessageType.HistoricalError => throw (Exception)message.Value!,
                MessageType.HistoricalCompleted => HandleHistoricalCompletion(liveBuffer),
                _ => throw new InvalidOperationException($"Unknown message: '{message}'."),
            };
        }

        private List<TValue> HandleHistoricalCompletion(List<TValue> buffer)
        {
            Handler = LiveHandler;
            return buffer;
        }

        private static IList<TValue> HandleLiveMessage(List<TValue> buffer, TValue value)
        {
            buffer.Add(value);
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
        .SelectMany(state => state.Return); // TODO try Maybe<T> + where 

    private static Concat<TValue> HandleNextMessage<TValue>(Concat<TValue> previous, Message message)
        => previous with { Return = previous.State.Handler(message) };

    private static IObservable<Message> GetLiveMessages<TValue>(IObservable<TValue> live)
        => live.Select(live => new Message(MessageType.Live, live));

    private static IObservable<Message> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .ToList()
        .Materialize()
        .Select(notification => notification.Kind switch
        {
            NotificationKind.OnNext => new Message(MessageType.Historical, notification.Value),
            NotificationKind.OnError => new Message(MessageType.HistoricalError, notification.Exception),
            NotificationKind.OnCompleted => new Message(MessageType.HistoricalCompleted, null),
            _ => throw new InvalidOperationException($"Unknown notification: '{notification}'."),
        });
}

// tried replacing ConcatState<> with Func<Message<TValue>, Concat<TValue>> but got stuck on initializing HistoryAndLiveHandler
//private readonly record struct Concat<TValue>(IList<TValue> Return, Func<Message<TValue>, Concat<TValue>> Handler)
//{
//    public static Concat<TValue> Initial { get; } = new(Array.Empty<TValue>(), HistoryAndLiveHandler());

//    private readonly static Func<Message<TValue>, Concat<TValue>> liveHandler
//        = (Message<TValue> message) => new(new[] { (TValue)message.Value! }, liveHandler!);

//    private Func<Message<TValue>, Concat<TValue>> HistoryAndLiveHandler()
//    {
//        var @this = this;
//        List<TValue> liveBuffer = new();
//        return (Message<TValue> message)
//            => message.Type switch
//            {
//                MessageType.Live => HandleLiveMessage(liveBuffer, (TValue)message.Value!, @this.Handler),
//                MessageType.Historical => new((IList<TValue>)message.Value!, @this.Handler),
//                MessageType.HistoricalError => throw (Exception)message.Value!,
//                MessageType.HistoricalCompleted => HandleHistoricalCompletion(liveBuffer),
//                _ => throw new InvalidOperationException($"Unknown message: '{message}'."),
//            };
//    }

//    private static Concat<TValue> HandleHistoricalCompletion(List<TValue> buffer)
//    {
//        return new(buffer, liveHandler);
//    }

//    private static Concat<TValue> HandleLiveMessage(List<TValue> buffer, TValue value, Func<Message<TValue>, Concat<TValue>> handler)
//    {
//        buffer.Add(value);
//        return new(Array.Empty<TValue>(), handler);
//    }
//}