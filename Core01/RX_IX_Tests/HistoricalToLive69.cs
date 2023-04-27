using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MarcinGajda.RX_IX_Tests;
public static class HistoricalToLive69
{
    private enum MessageType : byte
    {
        Live = 0,
        Historical,
        HistoricalCompleted,
        HistoricalError,
    }

    private readonly record struct Message<TValue>(MessageType Type, TValue? Value, Exception? Exception);

    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
    {
        var values = new Subject<TValue>();
        var subscription = GetLiveMessages(live)
            .Merge(GetHistoricalMessages(historical))
            .Subscribe(
                SubscriptionAction(values),
                exception =>
                {
                    values.OnError(exception);
                    values.Dispose();
                },
                () =>
                {
                    values.OnCompleted();
                    values.Dispose();
                });

        return values.Finally(subscription.Dispose);
    }

    private static Action<Message<TValue>> SubscriptionAction<TValue>(Subject<TValue> values)
    {
        var isHistoryFinished = false;
        var liveBuffer = new List<TValue>();
        return next =>
        {
            switch (next.Type)
            {
                case MessageType.Live:
                    if (isHistoryFinished)
                    {
                        values.OnNext(next.Value!);
                    }
                    else
                    {
                        liveBuffer.Add(next.Value!);
                    }
                    break;
                case MessageType.Historical:
                    values.OnNext(next.Value!);
                    break;
                case MessageType.HistoricalCompleted:
                    isHistoryFinished = true;
                    foreach (var item in liveBuffer)
                    {
                        values.OnNext(item);
                    }
                    liveBuffer = null;
                    break;
                case MessageType.HistoricalError:
                    values.OnError(next.Exception!);
                    break;

            }
        };
    }

    private static IObservable<Message<TValue>> GetLiveMessages<TValue>(IObservable<TValue> live)
        => live.Select(live => new Message<TValue>(MessageType.Live, live, null));

    private static IObservable<Message<TValue>> GetHistoricalMessages<TValue>(IObservable<TValue> historical)
        => historical
        .Materialize()
        .Select(notification => notification.Kind switch
        {
            NotificationKind.OnNext => new Message<TValue>(MessageType.Historical, notification.Value, null),
            NotificationKind.OnError => new Message<TValue>(MessageType.HistoricalError, default, notification.Exception!),
            NotificationKind.OnCompleted => new Message<TValue>(MessageType.HistoricalCompleted, default, null),
            _ => throw new InvalidOperationException($"Unknown notification: '{notification}'."),
        });
}