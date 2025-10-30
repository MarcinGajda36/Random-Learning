
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
        HistoricalCompleted,
        HistoricalError,
    }

    private readonly record struct Message<TValue>(MessageType Type, IEnumerable<TValue> Values, Exception? Exception);

    private sealed class ConcatState<TValue>
    {
        private List<TValue>? liveBuffer = [];

        public IEnumerable<TValue> HandleNextMessage(in Message<TValue> message)
            => message switch
            {
                { Type: MessageType.Live, Values: var values } => HandleLiveMessage(values),
                { Type: MessageType.Historical, Values: var values } => values,
                { Type: MessageType.HistoricalCompleted } => HandleHistoricalCompletion(),
                { Type: MessageType.HistoricalError, Exception: { } exception } => throw exception,
                var unknown => throw new InvalidOperationException($"Unknown message: '{unknown}'."),
            };

        private List<TValue> HandleHistoricalCompletion()
        {
            var buffered = liveBuffer;
            liveBuffer = null;
            return buffered!;
        }

        private IEnumerable<TValue> HandleLiveMessage(IEnumerable<TValue> values)
        {
            if (liveBuffer == null)
            {
                return values;
            }
            liveBuffer.AddRange(values);
            return [];
        }
    }

    private readonly record struct Concat<TValue>(IEnumerable<TValue> Return, ConcatState<TValue> State);

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historicalObservable)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historicalObservable))
        .HandleConcat();

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IEnumerable<TValue> historicalEnumerable)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historicalEnumerable))
        .HandleConcat();

    private static IObservable<TValue> HandleConcat<TValue>(this IObservable<Message<TValue>> merged)
        => merged
        .Scan(
            new Concat<TValue>([], new ConcatState<TValue>()),
            static (previous, message) => HandleNextMessage(in previous, in message))
        .SelectMany(state => state.Return);

    private static Concat<TValue> HandleNextMessage<TValue>(in Concat<TValue> previous, in Message<TValue> message)
        => previous with { Return = previous.State.HandleNextMessage(in message) };

    private static IObservable<Message<TValue>> GetLiveMessages<TValue>(IObservable<TValue> live)
        => live.Select(live => new Message<TValue>(MessageType.Live, [live], null));

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .Materialize()
        .Select(notification => notification switch
        {
            { Kind: NotificationKind.OnNext, Value: var value } => new Message<TValue>(MessageType.Historical, [value], null),
            { Kind: NotificationKind.OnError, Exception: { } exception } => new Message<TValue>(MessageType.HistoricalError, [], exception),
            { Kind: NotificationKind.OnCompleted } => new Message<TValue>(MessageType.HistoricalCompleted, [], null),
            var unknown => throw new InvalidOperationException($"Unknown notification: '{unknown}'."),
        });

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IEnumerable<TValue> historical)
        => Observable
        .Return(new Message<TValue>(MessageType.Historical, historical, null))
        .Append(new Message<TValue>(MessageType.HistoricalCompleted, [], null));
}