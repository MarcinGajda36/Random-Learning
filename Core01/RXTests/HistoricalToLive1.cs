using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RXTests;

public static class HistoricalToLive
{
    private interface IMessage { }
    private sealed record Live<TValue>(TValue Value) : IMessage;
    private sealed record Historical<TValue>(TValue Value) : IMessage;
    private sealed record HistoricalCompleted() : IMessage;
    private sealed record HistoricalError(Exception Exception) : IMessage;

    private sealed record ConcatState<TValue>(
        ImmutableQueue<TValue> LiveBuffer,
        bool HasHistoricalEnded,
        IObservable<TValue> AvailableMessages)
    {
        public static readonly ConcatState<TValue> Empty
            = new(ImmutableQueue<TValue>.Empty, false, Observable.Empty<TValue>());
    }

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(ConcatState<TValue>.Empty, HandleNextMessage)
        .Select(state => state.AvailableMessages)
        .Concat();

    private static ConcatState<TValue> HandleNextMessage<TValue>(ConcatState<TValue> state, IMessage message)
        => (state, message) switch
        {
            ({ HasHistoricalEnded: true }, Live<TValue>(var live))
                => state with { AvailableMessages = Observable.Return(live) },
            ({ HasHistoricalEnded: false }, Live<TValue>(var live))
                => state with { AvailableMessages = Observable.Empty<TValue>(), LiveBuffer = state.LiveBuffer.Enqueue(live) },
            (_, Historical<TValue>(var historical))
                => state with { AvailableMessages = Observable.Return(historical) },
            (_, HistoricalCompleted)
                => state with { AvailableMessages = Observable.ToObservable(state.LiveBuffer), LiveBuffer = ImmutableQueue<TValue>.Empty, HasHistoricalEnded = true },
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

public static class HistoricalToLive1_Dedup
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
        IEqualityComparer<TValue>? Comparer)
    // Is comparer and de-duping needed?
    // It can be harmful in scenario: 
    // 1) Live returns Obj1_ver1 -> pushed to buffer
    // 2) Live returns Obj1_ver2 -> pushed to buffer
    // 3) History returns Obj1_ver2 -> Obj1_ver2 removed from buffer
    // 4) User gets Historical(Obj1_ver2) Then older Live(Obj1_ver1)
    // This can be corrected with more advanced de-dup logic
    //  -> Comparer can detect dup as TValue.Id == Buffer.TValue.Id && TValue.ModifiedAt >= Buffer.TValue.ModifiedAt
    // but is it worth it?
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