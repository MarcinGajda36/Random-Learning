using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Actors;

public abstract class StatefulOneWayBlockBase<TState, TInput>
    : ITargetBlock<TInput>
{
    private readonly ActionBlock<TInput> block;
    protected TState State { get; private set; }

    public StatefulOneWayBlockBase(
        TState startingState,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    {
        State = startingState;
        block = CreateBlock(executionDataflowBlockOptions);
    }

    protected abstract ValueTask<TState> Operation(TState state, TInput input);

    private ActionBlock<TInput> CreateBlock(ExecutionDataflowBlockOptions? executionDataflowBlockOptions)
        => new(
            async input => State = await Operation(State, input),
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

public class StatefulOneWayBlock<TState, TInput>(
    TState startingState,
    Func<TState, TInput, ValueTask<TState>> operation,
    ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    : StatefulOneWayBlockBase<TState, TInput>(startingState, executionDataflowBlockOptions)
{
    protected override ValueTask<TState> Operation(TState state, TInput input)
        => operation(state, input);
}

public static class StatefulOneWayBlock
{
    public static StatefulOneWayBlock<TState, TInput> Create<TState, TInput>(
        TState startingState,
        Func<TState, TInput, ValueTask<TState>> operation,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        => new(startingState, operation, executionDataflowBlockOptions);

    public static StatefulOneWayBlock<TState, TInput> Create<TState, TInput>(
        TState startingState,
        Func<TState, TInput, TState> operation,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        => new(
            startingState,
            (state, input) => ValueTask.FromResult(operation(state, input)),
            executionDataflowBlockOptions);
}