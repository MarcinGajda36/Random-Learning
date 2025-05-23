﻿namespace MarcinGajda.Actors;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public abstract class StatefulOneWayAsyncBlockBase<TState, TInput>
    : ITargetBlock<TInput>
{
    private readonly ActionBlock<TInput> block;
    private readonly CancellationToken blockCancellation;
    protected TState State { get; private set; }

    public StatefulOneWayAsyncBlockBase(
        TState startingState,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    {
        State = startingState;
        block = CreateBlock(executionDataflowBlockOptions);
        blockCancellation = executionDataflowBlockOptions?.CancellationToken ?? CancellationToken.None;
    }

    protected abstract ValueTask<TState> OperationAsync(TState state, TInput input, CancellationToken cancellationToken);

    private ActionBlock<TInput> CreateBlock(ExecutionDataflowBlockOptions? executionDataflowBlockOptions)
        => new(
            async input => State = await OperationAsync(State, input, blockCancellation),
            executionDataflowBlockOptions ?? new());

    public Task Completion
        => block.Completion;

    public void Complete()
        => block.Complete();

    DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
        => ((ITargetBlock<TInput>)block).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    void IDataflowBlock.Fault(Exception exception)
        => ((IDataflowBlock)block).Fault(exception);
}

public class StatefulOneWayAsyncBlock<TState, TInput>(
    TState startingState,
    Func<TState, TInput, CancellationToken, ValueTask<TState>> operationAsync,
    ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    : StatefulOneWayAsyncBlockBase<TState, TInput>(startingState, executionDataflowBlockOptions)
{
    protected override ValueTask<TState> OperationAsync(TState state, TInput input, CancellationToken cancellationToken)
        => operationAsync(state, input, cancellationToken);
}

public static class StatefulOneWayAsyncBlock
{
    public static StatefulOneWayAsyncBlock<TState, TInput> Create<TState, TInput>(
        TState startingState,
        Func<TState, TInput, CancellationToken, ValueTask<TState>> operationAsync,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        => new(startingState, operationAsync, executionDataflowBlockOptions);
}