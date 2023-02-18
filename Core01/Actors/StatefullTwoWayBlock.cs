using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Actors;

public abstract class StatefulTwoWayBlockBase<TState, TInput, TOutput>
    : IPropagatorBlock<TInput, TOutput>
{
    private readonly TransformBlock<TInput, TOutput> block;
    private TState state;

    public StatefulTwoWayBlockBase(
        TState startingState,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    {
        state = startingState;
        block = CreateBlock(executionDataflowBlockOptions);
    }

    protected abstract Task<(TState, TOutput)> Operation(TState state, TInput input);

    private TransformBlock<TInput, TOutput> CreateBlock(ExecutionDataflowBlockOptions? executionDataflowBlockOptions)
        => new(
            async input =>
            {
                (state, var output) = await Operation(state, input);
                return output;
            },
            executionDataflowBlockOptions ?? new());

    public Task Completion
        => block.Completion;

    public void Complete()
        => block.Complete();

    public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        => block.LinkTo(target, linkOptions);

    TOutput? ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        => ((ISourceBlock<TOutput>)block).ConsumeMessage(messageHeader, target, out messageConsumed);
    void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)block).ReleaseReservation(messageHeader, target);
    bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)block).ReserveMessage(messageHeader, target);
    DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
        => ((ITargetBlock<TInput>)block).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    void IDataflowBlock.Fault(Exception exception)
        => ((IDataflowBlock)block).Fault(exception);
}

public sealed class StatefullTwoWayBlock<TState, TInput, TOutput>
    : StatefulTwoWayBlockBase<TState, TInput, TOutput>
{
    private readonly Func<TState, TInput, Task<(TState, TOutput)>> operation;

    public StatefullTwoWayBlock(
        TState startingState,
        Func<TState, TInput, Task<(TState, TOutput)>> operation,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        : base(startingState, executionDataflowBlockOptions)
        => this.operation = operation;

    protected override Task<(TState, TOutput)> Operation(TState state, TInput input)
        => operation(state, input);
}

public static class StatefullTwoWayBlock
{
    public static StatefullTwoWayBlock<TState, TInput, TOutput> Create<TState, TInput, TOutput>(
        TState startingState,
        Func<TState, TInput, Task<(TState, TOutput)>> operation,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        => new(startingState, operation, executionDataflowBlockOptions);

    public static StatefullTwoWayBlock<TState, TInput, TOutput> Create<TState, TInput, TOutput>(
        TState startingState,
        Func<TState, TInput, (TState, TOutput)> operation,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        => new(
            startingState,
            (state, input) => Task.FromResult(operation(state, input)),
            executionDataflowBlockOptions);

    public static void Test()
    {
        var actor = Create("startState", (string currentState, int input) => (currentState + "newState", input * 10));
        _ = actor.AsObservable();
    }
}
