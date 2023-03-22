﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RXTests;
public static class HistoricalToLive2
{
    internal enum MessageType : byte
    {
        Live = 0,
        Historical,
        HistoricalError,
        HistoricalCompleted,
    }

    internal readonly record struct Message<TValue>(MessageType Type, IList<TValue> Value, Exception? Exception);

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

    internal sealed class ConcatState<TValue>
    {
        private List<TValue>? liveBuffer;
        private bool hasHistoricalEnded;

        public IList<TValue> HandleNextMessage(Message<TValue> message)
        {
            switch (message.Type)
            {
                case MessageType.Live:
                    if (hasHistoricalEnded)
                    {
                        return message.Value;
                    }

                    liveBuffer ??= new List<TValue>();
                    liveBuffer.AddRange(message.Value);
                    return Array.Empty<TValue>();

                case MessageType.Historical:
                    return message.Value;

                case MessageType.HistoricalError:
                    throw message.Exception!;

                case MessageType.HistoricalCompleted:
                    hasHistoricalEnded = true;
                    if (liveBuffer is null)
                    {
                        return Array.Empty<TValue>();
                    }
                    var buffered = liveBuffer;
                    liveBuffer = null;
                    return buffered;

                default:
                    throw new InvalidOperationException($"Unknown message: '{message}'.");
            }
        }
    }

    internal readonly record struct Concat<TValue>(IList<TValue> Return, ConcatState<TValue> State);

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
        => GetLiveMessages(live)
        .Merge(GetHistoricalMessages(historical))
        .Scan(
            new Concat<TValue>(Array.Empty<TValue>(), new ConcatState<TValue>()),
            HandleNextMessage)
        .SelectMany(state => state.Return);

    internal static Concat<TValue> HandleNextMessage<TValue>(Concat<TValue> previous, Message<TValue> message)
        => previous with { Return = previous.State.HandleNextMessage(message) };

    private static IObservable<Message<TValue>> GetLiveMessages<TValue>(IObservable<TValue> live)
        => live.Buffer(TimeSpan.FromMilliseconds(16d)).Select(Message.Live);

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .ToArray()
        .Materialize()
        .Select(notification => notification.Kind switch
        {
            NotificationKind.OnNext => Message.Historical(notification.Value),
            NotificationKind.OnError => Message.HistoricalError<TValue>(notification.Exception),
            NotificationKind.OnCompleted => Message.HistoricalCompleted<TValue>(),
            _ => throw new InvalidOperationException($"Unknown notification: '{notification}'."),
        });
}