namespace MarcinGajda.RX_IX_Tests;

using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

internal class HistoricalToLive_Forgetting
{
    public static IObservable<TValue> ConcatLiveAfterHistory<TValue>(
        IObservable<TValue> live,
        IObservable<TValue> historical)
    {
        var historyChannel = Channel.CreateUnbounded<TValue>();
        var liveChannel = Channel.CreateUnbounded<TValue>();
        return Observable.Create<TValue>(async (observer, cancelationToken) =>
        {
            using var historySubscription = historical.Subscribe(
                (value) => historyChannel.Writer.TryWrite(value),
                (exception) => historyChannel.Writer.TryComplete(exception),
                () => historyChannel.Writer.TryComplete());

            using var liveSubscription = live.Subscribe(
                (value) => liveChannel.Writer.TryWrite(value),
                (exception) => liveChannel.Writer.TryComplete(exception),
                () => liveChannel.Writer.TryComplete());

            await ConsumeChannel(observer, historyChannel.Reader, cancelationToken);
            await ConsumeChannel(observer, liveChannel.Reader, cancelationToken);
        });
    }

    private static async Task ConsumeChannel<TValue>(IObserver<TValue> observer, ChannelReader<TValue> channel, CancellationToken cancelationToken)
    {
        while (await channel.WaitToReadAsync(cancelationToken))
        {
            while (channel.TryRead(out var value))
            {
                observer.OnNext(value);
            }
        }
        var completion = channel.Completion;
        if (completion.IsFaulted)
        {
            observer.OnError(completion.Exception);
        }
    }
}
