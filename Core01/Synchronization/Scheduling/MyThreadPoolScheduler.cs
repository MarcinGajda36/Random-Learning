using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;
internal sealed class MyThreadPoolScheduler : TaskScheduler
{
    const int QueueSize = 32_768;
    const uint WrapAroundMask = QueueSize - 1;
    readonly Task?[] queue = new Task?[QueueSize];
    readonly Thread[] threads;


    readonly int enqueueIndexBitShift;
    uint queueInfo;
    uint QueueInfo => Volatile.Read(ref queueInfo);
    uint Count => QueueInfo & WrapAroundMask;
    uint EnqueueIndex => (QueueInfo & ~WrapAroundMask) >> enqueueIndexBitShift;

    public MyThreadPoolScheduler(int threadCount = 4)
    {
        if (threadCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(threadCount), threadCount, "Thread count has to be bigger then 0.");
        }

        enqueueIndexBitShift = BitOperations.TrailingZeroCount(QueueSize);
        var perThread = QueueSize / threadCount;
        threads = new Thread[threadCount];
        for (int index = 0; index < threadCount; index++)
        {
            var thread = new Thread(worker => ((Worker)worker!).Work());
            threads[index] = thread;
            thread.Start(new Worker(
                this,
                index * perThread, // TODO Can i have them start at 0..4 and have 4 index step?
                (index + 1) * perThread));
        }
    }

    protected override void QueueTask(Task task)
    {
        SpinWait spinWait = new SpinWait();
        while (true)
        {
            var info = queueInfo;
            if (Interlocked.CompareExchange(ref queue[EnqueueIndex], task, null) == null)
            {
                return;
            }
            Interlocked.CompareExchange(ref queueInfo, NextEnqueueIndex(info), info);
            spinWait.SpinOnce();
        }
    }

    private static uint NextEnqueueIndex(uint current)
        => 0; // TODO

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

    class Worker
    {
        private readonly MyThreadPoolScheduler parent;
        private readonly int start;
        private readonly int length;

        public Worker(MyThreadPoolScheduler parent, int start, int length)
        {
            this.parent = parent;
            this.start = start;
            this.length = length;
        }

        internal void Work()
        {
            var queueWindow = parent.queue.AsSpan(start, length);
            while (parent.Count > 0)
            {
                for (int i = 0; i < queueWindow.Length; i++)
                {
                    // First worker can be optimized to just try take right? maybe not
                    //if (Interlocked.Exchange(ref ) // TODO
                }
            }
        }
    }
}
