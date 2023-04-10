using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;
sealed class ThreadStickyTaskScheduler : TaskScheduler, IDisposable
{
    readonly record struct QueueEventPair(ConcurrentQueue<Task> Queue, ManualResetEventSlim EventSlim);

    const int MaxWorkers = 4; // 2,4,8 .. any power of 2 
    readonly SingleThreadScheduler[] workers = new SingleThreadScheduler[MaxWorkers];
    readonly ConcurrentQueue<Task>[] queues = new ConcurrentQueue<Task>[MaxWorkers];
    readonly QueueEventPair[] queueEventPairs = new QueueEventPair[MaxWorkers];
    const int QueueIndexMask = MaxWorkers - 1;

    public ThreadStickyTaskScheduler()
    {
        for (int index = 0; index < queues.Length; index++)
        {
            queues[index] = new ConcurrentQueue<Task>();
            workers[index] = new SingleThreadScheduler(index, this);
        }
        Array.ForEach(workers, worker => worker.Start());
    }

    protected override void QueueTask(Task task)
    {
        // work stealing with neighborQueue creates possibility for same queue tasks to be executed concurrently
        var index = Environment.CurrentManagedThreadId & QueueIndexMask;
        var pair = queueEventPairs[index];
        pair.Queue.Enqueue(task);
        if (pair.EventSlim.IsSet is false)
        {
            pair.EventSlim.Set();
        }
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (taskWasPreviouslyQueued is false)
        {
            return TryExecuteTask(task);
        }
        return false;
    }

    protected override IEnumerable<Task>? GetScheduledTasks()
        => queues.SelectMany(queue => queue);

    public override int MaximumConcurrencyLevel
        => workers.Length;

    public void Dispose()
        => Array.ForEach(workers, worker => worker.Dispose());

    class SingleThreadScheduler : IDisposable
    {
        readonly ThreadStickyTaskScheduler parent;
        readonly ConcurrentQueue<Task> queue; // I can try something like in SpiningPool
        readonly ManualResetEventSlim @event;
        readonly Thread thread;
        readonly CancellationTokenSource cancellation;
        readonly int index;
        bool previousNeighbor;

        ConcurrentQueue<Task>[] AllQueues => parent.queues;

        // How to replace ConcurrentQueue<Task>?
        // 1) Writing to some buffer (maybe array from pool)
        // the free worker could take entire buffer and queues would start a new one
        // but how to deal with buffer re-size?
        public SingleThreadScheduler(int index, ThreadStickyTaskScheduler parent)
        {
            this.index = index;
            this.parent = parent;
            queue = parent.queues[index];
            @event = new ManualResetEventSlim();
            parent.queueEventPairs[index] = new QueueEventPair(queue, @event);
            cancellation = new CancellationTokenSource();
            thread = new Thread(state => ((SingleThreadScheduler)state!).Schedule());
        }

        public void Start()
            => thread.Start(this);

        void Schedule()
        {
            var token = cancellation.Token;
            while (token.IsCancellationRequested is false)
            {
                CurrentQueue();
                HelpNeighbor();

                if (queue.TryDequeue(out var task))
                {
                    parent.TryExecuteTask(task);
                }
                else
                {
                    @event.Wait(token);
                    if (token.IsCancellationRequested is false)
                    {
                        @event.Reset();
                    }
                }
            }
        }

        void CurrentQueue()
        {
            while (queue.TryDequeue(out var task))
            {
                parent.TryExecuteTask(task);
            }
        }

        ConcurrentQueue<Task> GetNeighborQueue()
        {
            previousNeighbor = !previousNeighbor;
            var neighbour = previousNeighbor ? (index - 1) : (index + 1);
            return AllQueues[neighbour & QueueIndexMask];
        }

        void HelpNeighbor()
        {
            var queue = GetNeighborQueue();
            int limit = 32;
            while (limit > 0 && queue.TryDequeue(out var task))
            {
                parent.TryExecuteTask(task);
                --limit;
            }
        }

        public void Dispose()
        {
            cancellation.Cancel();
            cancellation.Dispose();
            @event.Dispose();
        }

        // I was thinking about buffering some before working
        //struct Worker
        //{
        //    public const int BufferLength = 32;

        //    readonly RoundRobinTaskScheduler parent;
        //    readonly Task[] buffer = new Task[BufferLength];
        //    int buffered;

        //    public Worker(RoundRobinTaskScheduler parent)
        //        => this.parent = parent;

        //    public void AddToBuffer(Task task)
        //    {
        //        buffer[buffered++] = task;
        //        if (buffered == buffer.Length)
        //        {
        //            for (int index = 0; index < buffer.Length; index++)
        //            {
        //                ref var item = ref buffer[index];
        //                parent.TryExecuteTask(item);
        //                item = null;
        //            }
        //            buffered = 0;
        //        }
        //    }

        //    public void FinishBuffer()
        //    {
        //        for (int index = 0; index < buffered; index++)
        //        {
        //            ref var item = ref buffer[index];
        //            parent.TryExecuteTask(item);
        //            item = null;
        //        }
        //        buffered = 0;
        //    }
        //}
    }
}
