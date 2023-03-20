using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;
internal sealed class RoundRobinTaskScheduler : TaskScheduler
{
    const int MaxWorkers = 4; // 2,4,8?
    readonly Worker[] workers = new Worker[MaxWorkers];
    readonly ConcurrentQueue<Task>[] queues = new ConcurrentQueue<Task>[MaxWorkers];
    const int WrapAroundMask = MaxWorkers - 1;
    int index;

    public RoundRobinTaskScheduler()
    {
        for (int index = 0; index < workers.Length; index++)
        {
            var worker = new Worker(index, this);
            workers[index] = worker;
            queues[index] = new ConcurrentQueue<Task>();
        }
        Array.ForEach(workers, worker => worker.Start());
    }

    protected override void QueueTask(Task task)
    {
        var index = Interlocked.Increment(ref this.index) & WrapAroundMask;
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
                DoQueue(queue);
                if ((count & 15) == 15) // Every 16
                {
                    // Steal
                }
                else if ((count & 31) == 0) // Every 32
                {

                }
                else
                {
                    Thread.Sleep(3);
                }
            }
        }

        void DoQueue(ConcurrentQueue<Task> queue)
        {
            while (queue.TryDequeue(out var task))
            {
                parent.TryExecuteTask(task);
            }
        }

        void StealWork(int stealOffset)
        {
            int limit = 32;
        }
    }
}
