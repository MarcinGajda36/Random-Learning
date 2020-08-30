using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Joins;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LanguageExt;

namespace MarcinGajda.RXTests
{
    public static class Observables
    {
        public static void SubjectTest()
        {
            using var subject = new Subject<string>();
            subject.OnNext("a");
            using IDisposable consoleSub = subject.Subscribe(Console.WriteLine);
            subject.OnNext("b");
            subject.OnNext("c");
        }
        public static void SubjectTest1()
        {
            using var subject = new Subject<string>();
            subject.OnNext("a");
            subject.Subscribe(Console.WriteLine);
            subject.OnNext("b");
            subject.OnCompleted();
            subject.OnNext("c");
        }
        public static void ReplaySubjectTest()
        {
            using var subject = new ReplaySubject<string>(2);
            subject.OnNext("1");
            subject.OnNext("2");
            subject.OnNext("3");
            subject.Subscribe(Console.WriteLine);
            subject.OnNext("4");
        }

        public static void BehaviorSubjectTest()
        {
            using var subject = new BehaviorSubject<string>("a");
            subject.OnNext("b");
            subject.OnNext("c");
            subject.Subscribe(Console.WriteLine);
            subject.OnNext("d");
        }
        public static void AsyncSubjectTest()
        {
            using var subject = new AsyncSubject<string>();
            subject.OnNext("b");
            subject.OnNext("c");
            subject.Subscribe(Console.WriteLine);
            subject.OnNext("d");
            subject.OnCompleted();
        }
        public static async void Throws()
        {
            IObservable<int> throws = Observable.Throw<int>(new Exception());
            await throws;
        }
        public static async void Create()
        {
            IObservable<int> observable = Observable.Create<int>(observer =>
            {
                observer.OnNext(1);
                observer.OnNext(2);
                Thread.Sleep(100);
                observer.OnCompleted();
                //return Disposable.Create(observer.OnCompleted);//maybe like that ?
                return Disposable.Create(() => Console.WriteLine("observer unsub"));
            });
        }
        public static async void Create1()
        {
            IObservable<int> Empty = Observable.Create<int>(observer =>
            {
                observer.OnCompleted();
                return Disposable.Empty;
            });
            IObservable<int> Never = Observable.Create<int>(observer => Disposable.Empty);
            IObservable<T> Throw<T>(Exception exception)
            {
                return Observable.Create<T>(observer =>
                {
                    observer.OnError(exception);
                    return Disposable.Empty;
                });
            }
            IObservable<T> Return<T>(T t)
            {
                return Observable.Create<T>(observer =>
                {
                    observer.OnNext(t);
                    observer.OnCompleted();
                    return Disposable.Empty;
                });
            }
        }
        public static void Generate()
        {
            Observable.Range(10, 15).Subscribe(Console.WriteLine);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            IObservable<string> MyRange(int start, int count)
            {
                int max = start + count;
                return Observable.Generate(
                    start,
                    state => state < max,
                    state => state + 1,
                    state => state.ToString());
            }
            MyRange(10, 15).Subscribe(Console.WriteLine);
        }
        public static void StartAction()
        {
            IObservable<int> start = Observable.Start(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    Thread.Sleep(1);
                }
                return 1;
            });
        }
        public static void Tests()
        {
            IObservable<IGroupedObservable<int, IEnumerable<int>>> a = Task.FromResult(new[] { 1, 2, 3 })
                .ToObservable()
                .GroupBy(i => i.Length)
                .SelectMany(async i =>
                {
                    int lang = await Task.FromResult(i.Key);
                    return Many(lang);
                })
                .GroupBy(many => many.First());

            static IEnumerable<int> Many(int x) => Enumerable.Range(0, x);
        }
        public static void MyAny<T>(IObservable<T> observable, Func<T, bool> condition)
        {
            IObservable<bool> any = Observable.Create<bool>(observer =>
            observable.Take(1).Subscribe(
                (_) =>
                {
                    observer.OnNext(true);
                    observer.OnCompleted();
                },
                () =>
                {
                    observer.OnNext(false);
                    observer.OnCompleted();
                }));
            IObservable<bool> conditionalAny = Observable.Create<bool>(observer =>
            observable.Where(condition).Take(1).Subscribe(
                (_) =>
                {
                    observer.OnNext(true);
                    observer.OnCompleted();
                },
                () =>
                {
                    observer.OnNext(false);
                    observer.OnCompleted();
                }));
        }
        public static void MyRunning()
        {
            IObservable<int> min = Observable.Range(0, 100)
                .Scan(Math.Min)
                .Distinct();

            IObservable<int> max = Observable.Range(0, 100)
                .Scan(int.MinValue, Math.Max)// int.MinValue probably not needed 
                .Distinct();
        }
        public static void BookRunningMinMax()
        {
            IObservable<T> Min<T>(IObservable<T> source)
            {

                Comparer<T> comparer = Comparer<T>.Default;
                T minOf(T x, T y) => comparer.Compare(x, y) < 0 ? x : y;
                return source.Scan(minOf).DistinctUntilChanged();
            }
        }
        public static void IAsyncEnumerableAndOb()
        {
            ValueTask<List<int>> list = Task.FromResult(1)
                .ToObservable()
                .SelectMany(Observable.Range)
                .ToAsyncEnumerable()
                .SelectAwait(async x => await Task.FromResult(x))
                .ToListAsync();
        }

        public static async Task AndThenWhen()
        {
            Pattern<int, int> pattern = Observable.Range(1, 10).And(Observable.Range(10, 10));
            Plan<int> plain = pattern.Then((left, right) => left + right);
            IObservable<int> sums = Observable.When(plain);
            using IDisposable sub = sums.Subscribe(Console.WriteLine);
            _ = await sums;
        }
        public static async void Randomm()
        {

            Observable.Range(1, 10)
                .ToAsyncEnumerable()
                .ToObservable();

            _ = Observable.Range(1, 10)
                .Materialize()
                .Dematerialize();

            var list1 = Task.FromResult(1)
                .ToObservable()
                .SelectMany(Observable.Range)
                .ToList()
                .ToTask();

            IQueryable<int> qu = Task.FromResult(1)
                .ToObservable()
                .AsQbservable()
                .ToQueryable();

            IObservable<int> manyAsync = Observable.Range(1, 10)
                .Select(async x => await Task.FromResult(x))
                .Merge();// .Merge() -> parallel | .Concat() -> sequencial

            IConnectableObservable<int> connectable = Observable.Range(1, 10).Publish();
            IObservable<int> refCount = connectable.RefCount();

            IConnectableObservable<long> replay = Observable.Interval(TimeSpan.FromSeconds(1))
                .Replay();
            IObservable<long> refCountReplay = replay.RefCount();

            IConnectableObservable<long> manualReplay = Observable.Interval(TimeSpan.FromSeconds(1))
                .Multicast(new ReplaySubject<long>());
            var counted = new RefCountDisposable(new CancellationTokenSource());
            var msgSenders = Task.FromResult(new[] { new { SenderId = "", MsgId = Guid.NewGuid() } });
            var r = await msgSenders
                .ToObservable()
                .SelectMany(msgSndr => msgSndr)
                .GroupBy(msgSndr => msgSndr.SenderId, msgSndr => msgSndr.MsgId)
                .SelectMany(async sndrGroup => (sndrGroup.Key, await sndrGroup.ToList()))
                .ToDictionary(sndrGroup => sndrGroup.Key, sndrGroup => sndrGroup.Item2);

            IDictionary<int, IList<Site>> result = await GetSource()
                .GroupBy(o => o.SiteId)
                .SelectMany(async group => new { group.Key, List = await group.ToList() })
                .ToDictionary(group => group.Key, group => group.List);

        }
        public static IObservable<Site> GetSource()
        {
            return default;
        }
        public class Site
        {
            public int SiteId { get; set; }
        }

        public static void Schedulerss()
        {
            IScheduler schedule1 = Scheduler.ThreadPool;
            IScheduler scheduler = TaskPoolScheduler.Default;
            using var serialDisp = new SerialDisposable();
            Action<int, Action<int>> work = (x, self) =>
            {
                Console.WriteLine("Running");
                self(x);
            };

            IDisposable token = scheduler.Schedule(1, work);
            Console.ReadLine();
            Console.WriteLine("Canceling");
            token.Dispose();
            Console.WriteLine("Canceled");
        }
        public static void Windows()
        {
            var observable = MyWindow(Observable.Range(0, 10), 3);
            using var s1 = observable.Subscribe(obs => obs.Subscribe(Console.WriteLine));

            var observable1 = MyWindow1(Observable.Range(0, 10), 3);
            using var s2 = observable1.Subscribe(obs => obs.Subscribe(Console.WriteLine));
        }
        public static IObservable<IObservable<T>> MyWindow<T>(
            this IObservable<T> source,
            int count)
        {
            var shared = source.Publish().RefCount();
            var windowEdge = shared
                .Select((_, idx) => idx % count)
                .Where(mod => mod == 0)
                .Publish()
                .RefCount();
            return shared.Window(windowEdge, _ => windowEdge);
        }
        public static IObservable<IObservable<T>> MyWindow1<T>(
            this IObservable<T> source,
            int count)
        {
            var windowEdge = source
                .Select((_, idx) => idx % count)
                .Where(mod => mod == 0);
            return source.Window(windowEdge, _ => windowEdge);//not working becouse closing selector is resubscribed and unsubscriber each invocation?
        }
        public static void TestQ()
        {
            var source = Observable.Interval(TimeSpan.FromSeconds(1));
            var q = source.AsQbservable();
            Console.WriteLine(q.ToString());
            var sub = q.Subscribe(Console.WriteLine);
            Console.ReadKey();
        }

        public static void RefCounts()
        {
        }

        public static async Task ObservableColections()
        {
            var list = new List<int>();
            var obs = list.ToObservable();
            list.Add(3);
            using (var sub = obs.Subscribe(Console.WriteLine))
            {
                list.Add(2);
                await Task.Delay(100)
                    .ConfigureAwait(true);
            }
        }
        public static async Task ObservableBuff()
        {
            var list = new BufferBlock<int>();
            var obs = list.AsObservable();
            using (var sub = obs.Subscribe(Console.WriteLine))
            {
                list.Post(1);
                await Task.Delay(100)
                    .ConfigureAwait(true);
            }
        }
        public static Task BehaviourWindow()
        {
            return new BehaviorSubject<string>("test")
                .Window(TimeSpan.FromSeconds(1))
                .SelectMany(x => { return x; })
                .Select(x => { Console.WriteLine(x); return x; })
                .ToTask();
        }
    }
}
