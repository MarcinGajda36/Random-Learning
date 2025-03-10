namespace MarcinGajda.Actors;

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public abstract class StatefulTwoWayAsyncBlockBase<TState, TInput, TOutput>
    : IPropagatorBlock<TInput, TOutput>
{
    private readonly TransformBlock<TInput, TOutput> block;
    protected TState State { get; private set; }

    public StatefulTwoWayAsyncBlockBase(
        TState startingState,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    {
        State = startingState;
        block = CreateBlock(executionDataflowBlockOptions);
    }

    protected abstract ValueTask<(TState, TOutput)> OperationAsync(TState state, TInput input);

    private TransformBlock<TInput, TOutput> CreateBlock(ExecutionDataflowBlockOptions? executionDataflowBlockOptions)
        => new(
            async input =>
            {
                (State, var output) = await OperationAsync(State, input);
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

public class StatefulTwoWayAsyncBlock<TState, TInput, TOutput>(
    TState startingState,
    Func<TState, TInput, ValueTask<(TState, TOutput)>> operationAsync,
    ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
    : StatefulTwoWayAsyncBlockBase<TState, TInput, TOutput>(startingState, executionDataflowBlockOptions)
{
    protected override ValueTask<(TState, TOutput)> OperationAsync(TState state, TInput input)
        => operationAsync(state, input);
}

public static class StatefulTwoWayAsyncBlock
{
    public static StatefulTwoWayAsyncBlock<TState, TInput, TOutput> Create<TState, TInput, TOutput>(
        TState startingState,
        Func<TState, TInput, ValueTask<(TState, TOutput)>> operationAsync,
        ExecutionDataflowBlockOptions? executionDataflowBlockOptions = null)
        => new(startingState, operationAsync, executionDataflowBlockOptions);

    public static void Test()
    {
        var actor = Create("startState", async (string currentState, int input) => (currentState + "newState", input * 10));
        _ = actor.AsObservable();
    }
}
