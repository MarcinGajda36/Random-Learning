using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;
internal sealed class MyThreadPoolScheduler : TaskScheduler
{
    const int QueueSize = 32_768;
    const int WrapAroundMask = QueueSize - 1;
    readonly Task?[] queue = new Task?[QueueSize];
    readonly ImmutableArray<Thread> threads;
    int enqueueIndex = 0;

    public MyThreadPoolScheduler(int threadCount = 4)
    {
        if (threadCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(threadCount), threadCount, "Thread count has to be bigger then 0.");
        }

        var threadsBuilder = ImmutableArray.CreateBuilder<Thread>(threadCount);
        for (int i = 0; i < threadCount; i++)
        {
            /*threadsBuilder.Add(new Thread()) */ // Take something with preferred index range 
        }
        threads = threadsBuilder.MoveToImmutable();
    }

    protected override void QueueTask(Task task)
    {
        SpinWait spinWait = new SpinWait();
        while (true)
        {
            int enqueIdx = Volatile.Read(ref enqueueIndex);
            if (Interlocked.CompareExchange(ref queue[enqueIdx], task, null) == null)
            {
                return;
            }
            Interlocked.CompareExchange(ref enqueueIndex, NextEnqueueIndex(enqueIdx), enqueIdx);
            spinWait.SpinOnce();
        }
    }

    private static int NextEnqueueIndex(int current)
        => ++current & WrapAroundMask;

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (taskWasPreviouslyQueued is false)
        {
            return TryExecuteTask(task);
        }
        var taskIndex = Array.IndexOf(queue, task);
        if (taskIndex != -1 && Interlocked.CompareExchange(ref queue[taskIndex], null, task) == task)
        {
            return TryExecuteTask(task);
        }
        return false;
    }

    protected override IEnumerable<Task>? GetScheduledTasks()
        => null;
}
