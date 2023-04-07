using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Joins;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LanguageExt;

namespace MarcinGajda.RX_IX_Tests
{
    public static class Observables
    {
        public static void SubjectTest()
        {
            using var subject = new Subject<string>();
            subject.OnNext("a");
            using var consoleSub = subject.Subscribe(Console.WriteLine);
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
            var throws = Observable.Throw<int>(new Exception());
            await throws;
        }
        public static async void Create()
        {
            var observable = Observable.Create<int>(observer =>
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
            var Empty = Observable.Create<int>(observer =>
            {
                observer.OnCompleted();
                return Disposable.Empty;
            });
            var Never = Observable.Create<int>(observer => Disposable.Empty);
            IObservable<T> Throw<T>(Exception exception) => Observable.Create<T>(observer =>
                                                                         {
                                                                             observer.OnError(exception);
                                                                             return Disposable.Empty;
                                                                         });
            IObservable<T> Return<T>(T t) => Observable.Create<T>(observer =>
                                                          {
                                                              observer.OnNext(t);
                                                              observer.OnCompleted();
                                                              return Disposable.Empty;
                                                          });
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
            var start = Observable.Start(() =>
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
            var a = Task.FromResult(new[] { 1, 2, 3 })
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
            var any = Observable.Create<bool>(observer =>
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
            var conditionalAny = Observable.Create<bool>(observer =>
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
            var min = Observable.Range(0, 100)
                .Scan(Math.Min)
                .Distinct();

            var max = Observable.Range(0, 100)
                .Scan(int.MinValue, Math.Max)// int.MinValue probably not needed 
                .Distinct();
        }
        public static void BookRunningMinMax()
        {
            IObservable<T> Min<T>(IObservable<T> source)
            {

                var comparer = Comparer<T>.Default;
                T minOf(T x, T y) => comparer.Compare(x, y) < 0 ? x : y;
                return source.Scan(minOf).DistinctUntilChanged();
            }
        }
        public static void IAsyncEnumerableAndOb()
        {
            var list = Task.FromResult(1)
                .ToObservable()
                .SelectMany(Observable.Range)
                .ToAsyncEnumerable()
                .SelectAwait(async x => await Task.FromResult(x))
                .ToListAsync();
        }

        public static async Task AndThenWhen()
        {
            var pattern = Observable.Range(1, 10)
                .And(Observable.Range(10, 10).Select(x => x.ToString()));
            var plain = pattern.Then((left, right) => left.ToString() + " " + right);
            var sums = Observable.When(plain);
            using var sub = sums.Subscribe(Console.WriteLine);
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

            var qu = Task.FromResult(1)
                .ToObservable()
                .AsQbservable()
                .ToQueryable();

            var manyAsync = Observable.Range(1, 10)
                .Select(async x => await Task.FromResult(x))
                .Merge();// .Merge() -> parallel | .Concat() -> sequencial

            var connectable = Observable.Range(1, 10).Publish();
            var refCount = connectable.RefCount();

            var replay = Observable.Interval(TimeSpan.FromSeconds(1))
                .Replay();
            var refCountReplay = replay.RefCount();

            var manualReplay = Observable.Interval(TimeSpan.FromSeconds(1))
                .Multicast(new ReplaySubject<long>());
            var counted = new RefCountDisposable(new CancellationTokenSource());
            var msgSenders = Task.FromResult(new[] { new { SenderId = "", MsgId = Guid.NewGuid() } });
            var r = await msgSenders
                .ToObservable()
                .SelectMany(msgSndr => msgSndr)
                .GroupBy(msgSndr => msgSndr.SenderId, msgSndr => msgSndr.MsgId)
                .SelectMany(async sndrGroup => (sndrGroup.Key, await sndrGroup.ToList()))
                .ToDictionary(sndrGroup => sndrGroup.Key, sndrGroup => sndrGroup.Item2);

            var result = await GetSource()
                .GroupBy(o => o.SiteId)
                .SelectMany(async group => new { group.Key, List = await group.ToList() })
                .ToDictionary(group => group.Key, group => group.List);

            var a = Observable
                .Create<int>(observer =>
                {
                    observer.OnNext(1);
                    observer.OnNext(2);
                    return Disposable.Create(observer.OnCompleted);
                });
            var asdasda = Observable.Return(new { a = 1 });
            var plain = Observable.Repeat("Path")
                .And(Observable.Repeat(1))
                .And(Observable.Repeat(new { a = 5 }));

            Observable.When(plain.Then((arg1, arg2, arg3) => arg3));
        }
        public static IObservable<Site> GetSource() => default;
        public class Site
        {
            public int SiteId { get; set; }
        }

        public static void Schedulerss()
        {
            var schedule1 = Scheduler.ThreadPool;
            IScheduler scheduler = TaskPoolScheduler.Default;
            using var serialDisp = new SerialDisposable();
            Action<int, Action<int>> work = (x, self) =>
            {
                Console.WriteLine("Running");
                self(x);
            };

            var token = scheduler.Schedule(1, work);
            Console.ReadLine();
            Console.WriteLine("Canceling");
            token.Dispose();
            Console.WriteLine("Canceled");
        }
        public static void Windows()
        {
            var observable = Observable.Range(0, 10).MyWindow(3);
            using var s1 = observable.Subscribe(obs => obs.Subscribe(Console.WriteLine));

            var observable1 = Observable.Range(0, 10).MyWindow1(3);
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
        public static Task BehaviourWindow() => new BehaviorSubject<string>("test")
                .Window(TimeSpan.FromSeconds(1))
                .SelectMany(x => { return x; })
                .Do(Console.WriteLine)
                .Select(x => { return x; })
                .ToTask();

        public static Task Splitt()
        {
            Observable.Range(0, 10)
                .Scan(ImmutableDictionary<int, string>.Empty,
                (accumulator, element) =>
                {
                    return accumulator.Add(element, element.ToString());
                })
                .Select(dict => dict);

            return Task.CompletedTask;
        }

        public static async Task ReadFile(string file)
        {
            var fileRead = Observable
                .FromAsync(() => File.ReadAllTextAsync(file));

        }

        public static async Task LinqQueryTests()
        {
            var elements =
                from x in Observable.Range(1, 2)
                from y in new[] { "Test1", "Test2" }.ToObservable()
                select (x, y);

            Console.WriteLine("elements");
            await elements.Do(x => Console.WriteLine(x));

            var query =
                from x in Observable.FromAsync(() => Task.FromResult(1))
                from y in Task.FromResult(x)
                from z in Observable.FromAsync(() => Task.FromResult(y))
                select (x, y, z);

            Console.WriteLine("query");
            await query.Do(x => Console.WriteLine(x));

            var whtType =
                from number in Enumerable.Range(0, 100).ToObservable()
                from asyncNumer in Observable.Return(number)
                from idk in Task.FromResult(number + asyncNumer)
                select idk;

            Console.WriteLine("whtType");
            await whtType.Do(x => Console.WriteLine(x));

        }
    }
}
