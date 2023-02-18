using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Actors;

public abstract class StatefulTwoWayActorBase<TState, TInput, TOutput>
    : IPropagatorBlock<TInput, TOutput>
{
    private readonly TransformBlock<TInput, TOutput> block;
    private TState state;

    public StatefulTwoWayActorBase(TState startingState)
    {
        state = startingState;
        block = CreateBlock();
    }

    protected abstract (TState, TOutput) Operation(TState state, TInput input);

    private TransformBlock<TInput, TOutput> CreateBlock()
        => new(input =>
        {
            (state, var output) = Operation(state, input);
            return output;
        });

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

public sealed class StatefullTwoWayActor<TState, TInput, TOutput>
    : StatefulTwoWayActorBase<TState, TInput, TOutput>
{
    private readonly Func<TState, TInput, (TState, TOutput)> operation;

    public StatefullTwoWayActor(TState startingState, Func<TState, TInput, (TState, TOutput)> operation)
        : base(startingState)
        => this.operation = operation;

    protected override (TState, TOutput) Operation(TState state, TInput input)
        => operation(state, input);
}

public static class StatefullTwoWayActor
{
    public static StatefullTwoWayActor<TState, TInput, TOutput> Create<TState, TInput, TOutput>(
        TState startingState,
        Func<TState, TInput, (TState, TOutput)> operation)
        => new(startingState, operation);

    public static void Test()
    {
        var actor = Create("startState", (string currentState, int input) => (currentState + "newState", input * 10));
        _ = actor.AsObservable();
    }
}
