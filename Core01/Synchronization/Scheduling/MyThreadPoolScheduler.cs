using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;
internal sealed class MyThreadPoolScheduler : TaskScheduler
{
    const int MaxThreads = 4; // 2,4,8?
    const int QueueSize = 32_768;
    const uint WrapAroundMask = QueueSize - 1;
    const uint CountMask = WrapAroundMask;
    const uint EnqueueIndexMask = ~CountMask;

    readonly int enqueueIndexBitShift = BitOperations.TrailingZeroCount(QueueSize);
    readonly Task?[] queue = new Task?[QueueSize];
    readonly Thread[] threads = new Thread[MaxThreads];

    uint queueInfo;

    public MyThreadPoolScheduler()
    {
        for (int index = 0; index < threads.Length; index++)
        {
            var thread = new Thread(worker => ((Worker)worker!).Work());
            threads[index] = thread;
            thread.Start(new Worker(this, index, MaxThreads));
        }
    }

    protected override void QueueTask(Task task)
    {
        SpinWait spinWait = new SpinWait();
        while (true)
        {
            var info = Volatile.Read(ref queueInfo);
            var enqueueIndex = GetEnqueueIndex(info);
            // How to handle case?
            // 2 Threads try to add, one lags behind and try idx = 0 and other is up-to date and tries idx = 1
            // idx 1 succeeds because its correct but idx 0 replaces null because thread pool manage to take task already
            // CompareExchange will make idx 0 compare invalid but we still replaced null so next if will fail
            if (Interlocked.CompareExchange(ref queue[enqueueIndex], task, null) == null)
            {
                Interlocked.CompareExchange(ref queueInfo, NextEnqueueIndex(info), info);
                return;
            }
            spinWait.SpinOnce();
        }
    }

    static uint GetQueueCount(uint queueInfo)
        => queueInfo & CountMask;
    uint GetEnqueueIndex(uint queueInfo)
        => (queueInfo & EnqueueIndexMask) >> enqueueIndexBitShift;

    uint NextEnqueueIndex(uint queueInfo)
    {
        var count = GetQueueCount(queueInfo) + 1;
        var enqueueIndex = GetEnqueueIndex(queueInfo);
        enqueueIndex = (enqueueIndex + 1) & WrapAroundMask;
        enqueueIndex <<= enqueueIndexBitShift;
        return enqueueIndex + count;
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
        => threads.Length;

    class Worker
    {
        private readonly MyThreadPoolScheduler parent;
        private readonly int start;
        private readonly int step;

        public Worker(MyThreadPoolScheduler parent, int start, int step)
        {
            this.parent = parent;
            this.start = start;
            this.step = step;
        }

        internal void Work()
        {
            // Notes: 
            // i can check ThreadPool.QueueUserWorkItem for ideas
            // should enqueue wake workers?
            // should worker wake next worker if he gets busy?

            var index = start;
            var queue = parent.queue;
            uint queueInfo;
            while ((queueInfo = Volatile.Read(ref parent.queueInfo)) > 0
                && index <= queue.Length)
            {
                for (int i = start; i < queue.Length; i += step)
                {
                    // First worker can be optimized to just try take right? maybe not
                    //if (Interlocked.Exchange(ref ) // TODO
                }
            }
        }
    }
}
