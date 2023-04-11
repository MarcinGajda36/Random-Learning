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

    private readonly record struct Message<TValue>(MessageType Type, object? Value);

    abstract class Handler<TValue>
    {
        public abstract Concat<TValue> Handle(Message<TValue> message); // Can pass by in
    }

    sealed class LiveHandler<TValue> : Handler<TValue>
    {
        public static readonly LiveHandler<TValue> Default = new();

        public override Concat<TValue> Handle(Message<TValue> message)
            => new(new[] { (TValue)message.Value! }, this);
    }

    sealed class HistoricalAndLiveHandler<TValue> : Handler<TValue>
    {
        private readonly List<TValue> liveBuffer = new();
        private Concat<TValue> HandleLiveMessage(TValue value)
        {
            liveBuffer.Add(value);
            return new(Array.Empty<TValue>(), this);
        }

        public override Concat<TValue> Handle(Message<TValue> message)
            => message.Type switch
            {
                MessageType.Live => HandleLiveMessage((TValue)message.Value!),
                MessageType.Historical => new((IList<TValue>)message.Value!, this),
                MessageType.HistoricalError => throw (Exception)message.Value!,
                MessageType.HistoricalCompleted => new(liveBuffer, LiveHandler<TValue>.Default),
                _ => throw new InvalidOperationException($"Unknown message: '{message}'."),
            };
    }

    private readonly record struct Concat<TValue>(IList<TValue> Return, Handler<TValue> Handler);

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(
            new Concat<TValue>(Array.Empty<TValue>(), new HistoricalAndLiveHandler<TValue>()),
            HandleNextMessage)
        .SelectMany(state => state.Return);

    private static Concat<TValue> HandleNextMessage<TValue>(Concat<TValue> previous, Message<TValue> message)
        => previous.Handler.Handle(message);

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