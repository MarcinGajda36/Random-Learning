using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using LanguageExt;


namespace MarcinGajda.WORK_observable
{
    public class NotifierHub1
    {
        private readonly NotifierHubContext1 _notifierHubContext1;
        private static readonly ConcurrentDictionary<string, IDisposable> _idNofier =
            new ConcurrentDictionary<string, IDisposable>();

        private readonly string contextId = "";

        public NotifierHub1(NotifierHubContext1 notifierHubContext1)
            => _notifierHubContext1 = notifierHubContext1;

        public void OnExit()
        {
            if (_idNofier.TryRemove(contextId, out var disposable))
            {
                disposable.Dispose();
            }
        }
        public void Observe(string path)
        {
            var sub = _notifierHubContext1.Sub(path);
            _ = _idNofier.TryAdd(contextId, sub);
        }
    }

    public class NotifierHubContext1
    {
        public NotifierHubContext1(/*Hub*/)
        {
        }

        public IDisposable Sub(string path) =>
            NotifierFactory2.GetOrAdd(path).Observable
                .Window(TimeSpan.FromSeconds(300))//can get Notifier2.TimeOut
                .SelectMany(Observable.LastOrDefaultAsync)
                .Where(notif => notif != null)
                .SelectMany(notif => /*send notif to Hub*/ Task.CompletedTask.ToObservable())
                .Subscribe();
    }

    public static class NotifierFactory2
    {
        private static readonly ConcurrentDictionary<string, Notifier2> _pathNotifier =
            new ConcurrentDictionary<string, Notifier2>();

        public static Notifier2 GetOrAdd(string path) =>
            _pathNotifier.GetOrAdd(path, newPath => new Notifier2(newPath));
    }

    public class Notifier2
    {
        public string Path { get; }
        private static readonly int Empty = 0;
        public IObservable<int> Observable =>
            _notifications;

        private readonly ISubject<int> _notifications =
            Subject.Synchronize(new BehaviorSubject<int>(Empty), TaskPoolScheduler.Default);

        public Notifier2(string path) => Path = path;

        public void Notify(int notif) =>
            _notifications.OnNext(notif);

        public void NotifyEnd() =>
            _notifications.OnNext(Empty);
    }
}
