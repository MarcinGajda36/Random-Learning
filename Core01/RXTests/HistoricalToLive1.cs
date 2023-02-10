using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RXTests;
internal static class HistoricalToLive1
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