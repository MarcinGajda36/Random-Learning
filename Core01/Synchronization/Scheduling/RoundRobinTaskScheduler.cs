using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;
internal sealed class RoundRobinTaskScheduler : TaskScheduler
{
    const int MaxWorkers = 4; // 4,8?
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
        readonly ConcurrentQueue<Task> queue;
        readonly Thread thread;
        readonly int index;

        ConcurrentQueue<Task>[] AllQueues => parent.queues;

        public Worker(int index, RoundRobinTaskScheduler parent)
        {
            this.parent = parent;
            this.index = index;
            queue = parent.queues[index];
            thread = new Thread(state => ((Worker)state!).Work());
        }

        public void Start() => thread.Start(this);

        void Work()
        {
            int count = -1;
            while (true)
            {
                ++count;
                DoQueue();
                if ((count & 15) == 15) // Every 16
                {
                    StealWork(1);
                }
                else if ((count & 31) == 0) // Every 32
                {
                    StealWork(2);
                }
                else if ((count & 3) == 3) // Every 4 except after (count & 15)
                {
                    Thread.Sleep(3);
                }
            }
        }

        void DoQueue()
        {
            while (queue.TryDequeue(out var task))
            {
                parent.TryExecuteTask(task);
            }
        }

        void StealWork(int offset)
        {
            var queueIndex = (index + offset) & (AllQueues.Length - 1);
            var queueToRob = AllQueues[queueIndex];
            int limit = 32;
            while (limit > 0 && queueToRob.TryDequeue(out var task))
            {
                parent.TryExecuteTask(task);
                --limit;
            }
        }
    }
}
