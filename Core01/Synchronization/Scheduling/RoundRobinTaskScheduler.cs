﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;
internal sealed class RoundRobinTaskScheduler : TaskScheduler
{
    const int MaxWorkers = 4; // 2,4,8 .. any power of 2 
    readonly SingleThreadScheduler[] workers = new SingleThreadScheduler[MaxWorkers];
    readonly ConcurrentQueue<Task>[] queues = new ConcurrentQueue<Task>[MaxWorkers];
    const int QueueIndexMask = MaxWorkers - 1;
    int index;

    public RoundRobinTaskScheduler()
    {
        for (int index = 0; index < queues.Length; index++)
        {
            queues[index] = new ConcurrentQueue<Task>();
        }
        for (int index = 0; index < workers.Length; index++) // Current worker impl grabs neighbor queue in ctor
        {
            workers[index] = new SingleThreadScheduler(index, this);
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

    class SingleThreadScheduler
    {
        readonly RoundRobinTaskScheduler parent;
        readonly ConcurrentQueue<Task> currentQueue; // I can try something like in SpiningPool
        readonly ConcurrentQueue<Task> neighborQueue;
        readonly Thread thread;
        readonly Worker worker;

        ConcurrentQueue<Task>[] AllQueues => parent.queues;

        // How to replace ConcurrentQueue<Task>?
        // 1) Writing to some buffer (maybe array from pool)
        // the free worker could take entire buffer and queues would start a new one
        // but how to deal with buffer re-size?
        public SingleThreadScheduler(int index, RoundRobinTaskScheduler parent)
        {
            this.parent = parent;
            currentQueue = parent.queues[index];
            var neighborQueueIndex = (index + 1) & QueueIndexMask;
            neighborQueue = AllQueues[neighborQueueIndex];
            thread = new Thread(state => ((SingleThreadScheduler)state!).Schedule());
            worker = new Worker(parent);
        }

        public void Start() => thread.Start(this);

        void Schedule()
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
            while (currentQueue.TryDequeue(out var task))
            {
                worker.AddToBuffer(task);
            }
            worker.FinishBuffer();
        }

        void HelpNeighbor()
        {
            int limit = Worker.BufferLength;
            while (limit > 0 && neighborQueue.TryDequeue(out var task))
            {
                worker.AddToBuffer(task);
                --limit;
            }
            worker.FinishBuffer();
        }

        struct Worker
        {
            public const int BufferLength = 32;

            readonly RoundRobinTaskScheduler parent;
            readonly Task[] buffer = new Task[BufferLength];
            int buffered;

            public Worker(RoundRobinTaskScheduler parent)
                => this.parent = parent;

            public void AddToBuffer(Task task)
            {
                buffer[buffered++] = task;
                if (buffered == buffer.Length)
                {
                    for (int index = 0; index < buffer.Length; index++)
                    {
                        ref var item = ref buffer[index];
                        parent.TryExecuteTask(item);
                        item = null;
                    }
                    buffered = 0;
                }
            }

            public void FinishBuffer()
            {
                for (int index = 0; index < buffered; index++)
                {
                    ref var item = ref buffer[index];
                    parent.TryExecuteTask(item);
                    item = null;
                }
                buffered = 0;
            }
        }
    }
}
