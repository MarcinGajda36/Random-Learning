﻿using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Actors;

public abstract class StatefullOneWayBlockBase<TState, TInput>
    : ITargetBlock<TInput>
{
    private readonly ActionBlock<TInput> block;
    private TState state;

    public StatefullOneWayBlockBase(
        TState startingState,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    {
        state = startingState;
        block = CreateBlock(executionDataflowBlockOptions);
    }

    protected abstract Task<TState> Operation(TState state, TInput input);

    private ActionBlock<TInput> CreateBlock(ExecutionDataflowBlockOptions? executionDataflowBlockOptions)
        => new(
            async input => state = await Operation(state, input),
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

public sealed class StatefullOneWayBlock<TState, TInput>
    : StatefullOneWayBlockBase<TState, TInput>
{
    private readonly Func<TState, TInput, Task<TState>> operation;

    public StatefullOneWayBlock(
        TState startingState,
        Func<TState, TInput, Task<TState>> operation,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        : base(startingState, executionDataflowBlockOptions)
        => this.operation = operation;

    protected override Task<TState> Operation(TState state, TInput input)
        => operation(state, input);
}

public static class StatefullOneWayBlock
{
    public static StatefullOneWayBlock<TState, TInput> Create<TState, TInput>(
        TState startingState,
        Func<TState, TInput, Task<TState>> operation,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        => new(startingState, operation, executionDataflowBlockOptions);

    public static StatefullOneWayBlock<TState, TInput> Create<TState, TInput>(
        TState startingState,
        Func<TState, TInput, TState> operation,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        => new(
            startingState,
            (state, input) => Task.FromResult(operation(state, input)),
            executionDataflowBlockOptions);
}