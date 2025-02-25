using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.WORK_observable;

public class WorkObservable
{
    public static readonly BufferBlock<(string, double)> bufferBlock =
        new BufferBlock<(string, double)>();

    private static ISourceBlock<(string Path, double Progress)> queue =>
        bufferBlock;

    public static async Task TestSplit(CancellationToken cancellationToken)
    {
        bufferBlock.Post(("", 1));

        var newest = queue
            .AsObservable()
            .GroupBy(pathProgres => pathProgres.Path) //first idea
            .SelectMany(pathGroups => pathGroups.Window(TimeSpan.FromMilliseconds(250)))
            .SelectMany(Observable.LastOrDefaultAsync)
            .Where(path => path != default);

        var newest1 = queue
            .AsObservable()
            .Window(TimeSpan.FromMilliseconds(250))
            .SelectMany(window => window.GroupBy(pathProgress => pathProgress.Path))
            .SelectMany(Observable.LastAsync);

        var sequencialPerPath = queue
            .AsObservable()
            .GroupBy(pathProgress => pathProgress.Path)
            .Select(group => group
                .Select(async pathProgress => pathProgress)
                .Concat()) //Seqiencial await inside single path group
            .Merge();//Concurrent await of diferent path groups

        newest.Subscribe(pathProgres => Console.WriteLine(bufferBlock.Count), cancellationToken);//third idea

        await Task.Delay(10000);
    }
}
public class Notifier : IDisposable
{
    private readonly ITargetBlock<int> _notifier;
    private readonly SerialDisposable _sub = new SerialDisposable();
    private IObservable<int> _notifs = Observable.Empty<int>();

    public Notifier(ITargetBlock<int> notifier) =>
        _notifier = notifier;

    public void Notify(IObservable<int> obs)
    {
        _notifs = _notifs.Merge(obs, TaskPoolScheduler.Default);
        _sub.Disposable = _notifs
            .Window(TimeSpan.FromMilliseconds(250))
            .SelectMany(Observable.LastOrDefaultAsync)
            .Where(x => x != default)
            .Subscribe(x => _notifier.Post(x));
    }

    public void Dispose() =>
        _sub.Dispose();
}

public class NotificationFactory
{
    private readonly int state;
    private readonly ActionBlock<int> notifier;

    public NotificationFactory(Func<int, Task> hub, int state)
    {
        this.state = state;
        notifier = new ActionBlock<int>(x => hub(x));
    }

    public Notifier Create() =>
        new Notifier(notifier);
}
