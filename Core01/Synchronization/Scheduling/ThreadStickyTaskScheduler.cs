﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Scheduling;

[StructLayout(LayoutKind.Sequential)]
sealed class ThreadStickyTaskScheduler : TaskScheduler, IDisposable
{
    const int DefaultMaxWorkers = 4;

    readonly int queueIndexMask;
    readonly ConcurrentQueue<Task>[] queues;
    readonly SingleThreadScheduler[] workers;

    public ThreadStickyTaskScheduler(int concurrencyLevel = DefaultMaxWorkers)
    {
        if (concurrencyLevel < 2 || BitOperations.IsPow2(concurrencyLevel) is false)
        {
            throw new ArgumentOutOfRangeException(nameof(concurrencyLevel), concurrencyLevel, "Expected at least 2 and a power of 2.");
        }

        queueIndexMask = concurrencyLevel - 1;
        queues = new ConcurrentQueue<Task>[concurrencyLevel];
        for (int index = 0; index < queues.Length; index++)
        {
            queues[index] = new();
        }

        workers = new SingleThreadScheduler[concurrencyLevel];
        for (int index = 0; index < workers.Length; index++)
        {
            workers[index] = new SingleThreadScheduler(index, this);
        }
        Array.ForEach(workers, worker => worker.Start());
    }

    protected override void QueueTask(Task task)
    {
        var index = Environment.CurrentManagedThreadId & queueIndexMask;
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
        => queues.SelectMany(queue => queue);

    public override int MaximumConcurrencyLevel
        => workers.Length;

    public void Dispose()
        => Array.ForEach(workers, worker => worker.Dispose());

    [StructLayout(LayoutKind.Sequential)]
    sealed class SingleThreadScheduler : IDisposable
    {
        // How to replace ConcurrentQueue<Task>?
        // I can try something like in 
        readonly ConcurrentQueue<Task> queue;
        readonly ThreadStickyTaskScheduler parent;
        readonly ConcurrentQueue<Task> neighborsQueue;

        readonly CancellationTokenSource cancellation;
        readonly Thread thread;

        public SingleThreadScheduler(int index, ThreadStickyTaskScheduler parent)
        {
            this.parent = parent;
            var queueIndexMask = parent.queueIndexMask;
            var nextIndex = (index + 1) & queueIndexMask;
            do
            {
                thread = new Thread(static state => ((SingleThreadScheduler)state!).Schedule());
                // Why loop?
                // 1) this prevents worst case that all threads enqueue to the same queue
                //  if Environment.CurrentManagedThreadId is used for enqueue
                // 2) if current thread needs to enqueue stillEmptyCheck then doing that on separate queue increases chances for parallelism
                // 3) i expect this to be one time cost at startup to increase performance at runtime
            } while ((thread.ManagedThreadId & queueIndexMask) != nextIndex);
            cancellation = new CancellationTokenSource();
            var queues = parent.queues;
            queue = queues[index];
            neighborsQueue = queues[nextIndex];
        }

        public void Start()
            => thread.Start(this);

        void Schedule()
        {
            var token = cancellation.Token;
            while (token.IsCancellationRequested is false)
            {
                while (queue.TryDequeue(out var task))
                {
                    parent.TryExecuteTask(task);
                }

                HelpNeighbor();

                if (queue.TryDequeue(out var stillEmptyCheck))
                {
                    parent.TryExecuteTask(stillEmptyCheck);
                }
                else
                {
                    // Tried ManualResetEventSlim to replace Thread.Sleep/Yield and it's cool but not for this implementation 
                    // because i like that thread without it's own elements help with neighborsQueue
                    Thread.Sleep(0);
                }
            }
        }

        void HelpNeighbor()
        {
            int limit = 32;
            while (limit > 0 && neighborsQueue.TryDequeue(out var task))
            {
                parent.TryExecuteTask(task);
                --limit;
            }
        }

        public void Dispose()
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }
    }
}

// Random thoughts: 

// ---------------------------------
// TODO I wonder if i can make singleton process and have other processes on the same machine talk to it via queues?
// look like this is it: https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-anonymous-pipes-for-local-interprocess-communication

// ---------------------------------
// 1 producer 
//  -> BufferBlock
//  new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = TaskScheduler.Default });
// 1 caching consumer 
//  -> 1 TransformBlock + BroadcastBlock (caching is cheap enough that pool is overkill)
//  -> or ActionBlock that posts to BroadcastBlock in func (if caching fails then linked blocks need filter logic)
//  new BroadcastBlock<int>(null, new DataflowBlockOptions { TaskScheduler = TaskScheduler.Default });
// 2 sending consumers
//  -> Here pool start to make sense right? 
//  -> maybe just 2? 2 feels bad because you get all multi-threating problems and not much concurrency, but it's bias right? 
//
// all block take scheduler so myScheduler can be used there
// but there is a risk that work gets terribly distributed and all threads schedule for 1 thread right? 

// ---------------------------------
// I was thinking about buffering some before working
//struct Worker
//{
//    public const int BufferLength = 32;

//    readonly RoundRobinTaskScheduler parent;
//    readonly Task[] buffer = new Task[BufferLength];
//    int buffered;

//    public Worker(RoundRobinTaskScheduler parent)
//        => this.parent = parent;

//    public void AddToBuffer(Task stillEmptyCheck)
//    {
//        buffer[buffered++] = stillEmptyCheck;
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