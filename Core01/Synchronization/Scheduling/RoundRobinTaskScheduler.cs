using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;
internal sealed class RoundRobinTaskScheduler : TaskScheduler
{
    const int MaxWorkers = 4; // 2,4,8 .. any power of 2 
    readonly Worker[] workers = new Worker[MaxWorkers];
    readonly ConcurrentQueue<Task>[] queues = new ConcurrentQueue<Task>[MaxWorkers];
    const int QueueIndexMask = MaxWorkers - 1;
    int index;

    public RoundRobinTaskScheduler()
    {
        for (int index = 0; index < workers.Length; index++)
        {
            workers[index] = new Worker(index, this);
            queues[index] = new ConcurrentQueue<Task>();
        }
        Array.ForEach(workers, worker => worker.Start());
    }

    protected override void QueueTask(Task task)
    {
        var index = Interlocked.Increment(ref this.index) & QueueIndexMask;
        queues[index].Enqueue(task);
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
        => null;

    public override int MaximumConcurrencyLevel
        => workers.Length;

    class Worker
    {
        readonly RoundRobinTaskScheduler parent;
        readonly ConcurrentQueue<Task> currentQueue; // I can try something like in SpiningPool
        readonly ConcurrentQueue<Task> neighborQueue;
        readonly Thread thread;

        ConcurrentQueue<Task>[] AllQueues => parent.queues;

        // How to replace ConcurrentQueue<Task>?
        // 1) Writing to some buffer (maybe array from pool)
        // the free worker could take entire buffer and queues would start a new one
        // but how to deal with buffer re-size?
        public Worker(int index, RoundRobinTaskScheduler parent)
        {
            this.parent = parent;
            currentQueue = parent.queues[index];
            var queueIndex = (index + 1) & QueueIndexMask;
            neighborQueue = AllQueues[queueIndex];
            thread = new Thread(state => ((Worker)state!).Work());
        }

        public void Start() => thread.Start(this);

        void Work()
        {
            while (true)
            {
                CurrentQueue();
                HelpNeighbor();

                if (currentQueue.TryDequeue(out var task))
                {
                    parent.TryExecuteTask(task);
                }
                else
                {
                    Thread.Yield();
                }
            }
        }

        void CurrentQueue()
        {
            // maybe buffer some tasks locally before executing? 
            // 1 re-used array should be enough 
            while (currentQueue.TryDequeue(out var task))
            {
                parent.TryExecuteTask(task);
            }
        }

        void HelpNeighbor()
        {
            var limit = 32;
            while (limit > 0 && neighborQueue.TryDequeue(out var task))
            {
                parent.TryExecuteTask(task);
                --limit;
            }
        }
    }
}
