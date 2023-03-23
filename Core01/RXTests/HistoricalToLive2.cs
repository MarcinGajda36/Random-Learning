﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MarcinGajda.RXTests;
public static class HistoricalToLive2
{
    private enum MessageType : byte
    {
        Live = 0,
        Historical,
        HistoricalError,
        HistoricalCompleted,
    }

    private readonly record struct Message<TValue>(MessageType Type, TValue[] Value, Exception? Exception);

    private static class Message
    {
        public static Message<TValue> Live<TValue>(TValue[] value)
            => new(MessageType.Live, value, null);

        public static Message<TValue> Historical<TValue>(TValue[] value)
            => new(MessageType.Historical, value, null);

        public static Message<TValue> HistoricalError<TValue>(Exception exception)
            => new(MessageType.HistoricalError, Array.Empty<TValue>(), exception);

        public static Message<TValue> HistoricalCompleted<TValue>()
            => new(MessageType.HistoricalCompleted, Array.Empty<TValue>(), null);
    }

    private sealed class ConcatState<TValue>
    {
        private List<TValue>? liveBuffer;
        private bool hasHistoricalEnded;

        public TValue[] HandleNextMessage(Message<TValue> message)
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
                    return buffered.ToArray();

                default:
                    throw new InvalidOperationException($"Unknown message: '{message}'.");
            }
        }
    }

    private readonly record struct Concat<TValue>(TValue[] Return, ConcatState<TValue> State);

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
        => live.Select(live => Message.Live(new[] { live }));

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