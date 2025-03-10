namespace MarcinGajda.Actors;

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public abstract class StatefulTwoWayBlockBase<TState, TInput, TOutput>
    : IPropagatorBlock<TInput, TOutput>
{
    private readonly TransformBlock<TInput, TOutput> block;
    protected TState State { get; private set; }

    public StatefulTwoWayBlockBase(
        TState startingState,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    {
        State = startingState;
        block = CreateBlock(executionDataflowBlockOptions);
    }

    protected abstract (TState, TOutput) Operation(TState state, TInput input);

    private TransformBlock<TInput, TOutput> CreateBlock(ExecutionDataflowBlockOptions? executionDataflowBlockOptions)
        => new(
            input =>
            {
                (State, var output) = Operation(State, input);
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

public class StatefulTwoWayBlock<TState, TInput, TOutput>(
    TState startingState,
    Func<TState, TInput, (TState, TOutput)> operation,
    ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    : StatefulTwoWayBlockBase<TState, TInput, TOutput>(startingState, executionDataflowBlockOptions)
{
    protected override (TState, TOutput) Operation(TState state, TInput input)
        => operation(state, input);
}

public static class StatefulTwoWayBlock
{
    public static StatefulTwoWayBlock<TState, TInput, TOutput> Create<TState, TInput, TOutput>(
        TState startingState,
        Func<TState, TInput, (TState, TOutput)> operation,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        => new(startingState, operation, executionDataflowBlockOptions);

    public static void Test()
    {
        var actor = Create("startState", (string currentState, int input) => (currentState + "newState", input * 10));
        _ = actor.AsObservable();
    }
}
