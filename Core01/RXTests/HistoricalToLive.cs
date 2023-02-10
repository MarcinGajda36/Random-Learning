using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RXTests;
internal static class HistoricalToLive
{
    private interface IMessage
    {
    }

    private sealed record EmptyMessage() : IMessage
    {
        public static readonly EmptyMessage Empty = new();
    }

    private sealed record Live<TValue>(TValue Value) : IMessage;
    private sealed record Historical<TValue>(TValue Value) : IMessage;
    private sealed record HistoricalCompleted() : IMessage;
    private sealed record HistoricalError(Exception Exception) : IMessage;

    private sealed record MessagesState<TValue>(
        ImmutableList<TValue> LiveValues,
        bool HasHistoricalEnded,
        IMessage LastMessage)
    {
        public static readonly MessagesState<TValue> Empty
            = new(ImmutableList<TValue>.Empty, false, EmptyMessage.Empty);
    }

    public static IObservable<TValue> Merge<TValue>(IObservable<TValue> live, IObservable<TValue> historical)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(MessagesState<TValue>.Empty, HandleNextMessage)
        .Select(GetCurrentMessages)
        .Concat();

    private static MessagesState<TValue> HandleNextMessage<TValue>(MessagesState<TValue> state, IMessage message)
        => (state, message) switch
        {
            ({ LastMessage: HistoricalCompleted }, Live<TValue> live) => state with { LastMessage = live, LiveValues = ImmutableList<TValue>.Empty },
            ({ HasHistoricalEnded: true }, Live<TValue> live) => state with { LastMessage = live },
            ({ HasHistoricalEnded: false }, Live<TValue> live) => state with { LastMessage = live, LiveValues = state.LiveValues.Add(live.Value) },
            (_, Historical<TValue> historical) => state with { LastMessage = historical, LiveValues = state.LiveValues.Remove(historical.Value) },// TODO add comparer for 'state.LiveValues.Remove'?
            (_, HistoricalCompleted endOfHistorical) => state with { LastMessage = endOfHistorical, HasHistoricalEnded = true },
            (_, HistoricalError(var exception)) => throw exception,
            _ => throw UnexpectedMergeState(nameof(HandleNextMessage), state, message)
        };

    private static IObservable<TValue> GetCurrentMessages<TValue>(MessagesState<TValue> state)
        => state switch
        {
            { HasHistoricalEnded: true, LastMessage: Live<TValue>(var live) } => Observable.Return(live),
            { HasHistoricalEnded: false, LastMessage: Live<TValue> } => Observable.Empty<TValue>(),
            { LastMessage: Historical<TValue>(var historical) } => Observable.Return(historical),
            { LastMessage: HistoricalCompleted } => Observable.ToObservable(state.LiveValues),
            _ => Observable.Throw<TValue>(UnexpectedMergeState(nameof(GetCurrentMessages), state, state.LastMessage))
        };

    private static InvalidOperationException UnexpectedMergeState<TValue>(string place, MessagesState<TValue> state, IMessage message)
        => throw new($"Unexpected '{place}' state inside: '{nameof(Merge)}', state: '{state}', message: '{message}'.");

    private static IObservable<Live<TValue>> GetLiveMessages<TValue>(IObservable<TValue> liveValues)
        => liveValues.Select(live => new Live<TValue>(live));

    private static IObservable<IMessage> GetHistoricalMessages<TValue>(IObservable<TValue> historicalValues)
        => historicalValues
        .Materialize()
        .Select<Notification<TValue>, IMessage>(notification => notification switch
        {
            { Kind: NotificationKind.OnNext, Value: var value } => new Historical<TValue>(value),
            { Kind: NotificationKind.OnError, Exception: var exception } => new HistoricalError(exception),
            { Kind: NotificationKind.OnCompleted } => new HistoricalCompleted(),
            _ => throw new NotSupportedException()
        });
}