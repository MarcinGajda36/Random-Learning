using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RXTests;
internal static class HistoricalToLive
{
    private interface IMessage { }
    private sealed record Live<TValue>(TValue Value) : IMessage;
    private sealed record Historical<TValue>(TValue Value) : IMessage;
    private sealed record HistoricalCompleted() : IMessage;
    private sealed record HistoricalError(Exception Exception) : IMessage;

    private sealed record ConcatState<TValue>(
        ImmutableList<TValue> LiveBuffer,
        bool HasHistoricalEnded,
        IObservable<TValue> AvailableMessages,
        IEqualityComparer<TValue>? Comparer) // Is comparer and de-duping needed?
    {
        public static ConcatState<TValue> Create(IEqualityComparer<TValue>? comparer)
            => new(ImmutableList<TValue>.Empty, false, Observable.Empty<TValue>(), comparer);
    }

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical,
        IEqualityComparer<TValue>? comparer = null)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(ConcatState<TValue>.Create(comparer), HandleNextMessage)
        .Select(state => state.AvailableMessages)
        .Concat();

    private static ConcatState<TValue> HandleNextMessage<TValue>(ConcatState<TValue> state, IMessage message)
        => (state, message) switch
        {
            ({ HasHistoricalEnded: true }, Live<TValue>(var live))
                => state with { AvailableMessages = Observable.Return(live) },
            ({ HasHistoricalEnded: false }, Live<TValue>(var live))
                => state with { AvailableMessages = Observable.Empty<TValue>(), LiveBuffer = state.LiveBuffer.Add(live) },
            (_, Historical<TValue>(var historical))
                => state with { AvailableMessages = Observable.Return(historical), LiveBuffer = state.LiveBuffer.Remove(historical, state.Comparer) },
            (_, HistoricalCompleted)
                => state with { AvailableMessages = Observable.ToObservable(state.LiveBuffer), LiveBuffer = ImmutableList<TValue>.Empty, HasHistoricalEnded = true },
            (_, HistoricalError(var exception))
                => throw exception,
            var unknown => throw new InvalidOperationException($"Unknown message state pair: '{unknown}'.")
        };

    private static IObservable<Live<TValue>> GetLiveMessages<TValue>(IObservable<TValue> live)
        => live.Select(live => new Live<TValue>(live));

    private static IObservable<IMessage> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .Materialize()
        .Select<Notification<TValue>, IMessage>(notification => notification switch
        {
            { Kind: NotificationKind.OnNext, Value: var value } => new Historical<TValue>(value),
            { Kind: NotificationKind.OnError, Exception: var exception } => new HistoricalError(exception),
            { Kind: NotificationKind.OnCompleted } => new HistoricalCompleted(),
            var unknown => throw new InvalidOperationException($"Unknown notification: '{unknown}'.")
        });
}

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

    private readonly record struct Concat<TValue>(IEnumerable<TValue> Values, ConcatState<TValue> State);
    private sealed class ConcatState<TValue>
    {
        // Mutable List and LinkedList don't use IEqualityComparer, is there order preserving mutable collection that takes IEqualityComparer?
        // private readonly IEqualityComparer<TValue>? comparer;  
        private List<TValue>? liveBuffer;
        public bool HasHistoricalEnded { get; private set; }

        public IEnumerable<TValue> HandleNextMessage(in Message<TValue> message)
        {
            if (message.Type is MessageType.Live)
            {
                if (HasHistoricalEnded)
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
                HasHistoricalEnded = true;
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

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(
            new Concat<TValue>(Enumerable.Empty<TValue>(), new ConcatState<TValue>()),
            (concat, message) => HandleNextMessage(in concat, in message))
        .SelectMany(state => state.Values);

    private static Concat<TValue> HandleNextMessage<TValue>(in Concat<TValue> state, in Message<TValue> message)
    {
        var values = state.State.HandleNextMessage(in message);
        return new(values, state.State);
    }

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