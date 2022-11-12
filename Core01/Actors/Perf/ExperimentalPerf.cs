using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Actors.Perf;

internal sealed class StateBag<TState>
{
    public TState State { get; set; }

    public StateBag(TState state)
        => State = state;
}

internal struct StateInputBag<TState, TInput>
{
    public StateBag<TState> StateBag { get; }
    public TInput Input { get; }

    public StateInputBag(StateBag<TState> stateBag, TInput input)
    {
        StateBag = stateBag;
        Input = input;
    }
}

public sealed class ExperimentalStatefulOneWayActor<TState, TInput, TOperation>
    where TOperation : struct, IOperationWithoutOutput<TState, TInput>
{
    private readonly ActionBlock<StateInputBag<TState, TInput>> @operator;
    private readonly StateBag<TState> stateBag;

    public ExperimentalStatefulOneWayActor(TState startingState)
    {
        stateBag = new(startingState);
        @operator = CreateOperator();
    }

    public Task Completion => @operator.Completion;

    private static ActionBlock<StateInputBag<TState, TInput>> CreateOperator()
        => new(static inputBag
            => inputBag.StateBag.State = default(TOperation).Execute(inputBag.StateBag.State, inputBag.Input));

    public bool Enqueue(TInput input)
        => @operator.Post(new(stateBag, input));
}

public sealed class ExperimentalStatefulTwoWayActor<TState, TInput, TOutput, TOperation>
    : ISourceBlock<TOutput>
    //: IPropagatorBlock<TInput, TOutput>
    where TOperation : struct, IOperationWithOutput<TState, TInput, TOutput>
{
    private readonly TransformBlock<StateInputBag<TState, TInput>, TOutput> @operator;
    private readonly StateBag<TState> stateBag;

    public ExperimentalStatefulTwoWayActor(TState startingState)
    {
        stateBag = new(startingState);
        @operator = CreateOperator();
    }

    public Task Completion => @operator.Completion;

    private static TransformBlock<StateInputBag<TState, TInput>, TOutput> CreateOperator()
        => new(static inputBag =>
        {
            (inputBag.StateBag.State, var output) = default(TOperation).Execute(inputBag.StateBag.State, inputBag.Input);
            return output;
        });

    public bool Enqueue(TInput input)
        => @operator.Post(new(stateBag, input));

    public void Complete() => @operator.Complete();
    public TOutput? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        => ((ISourceBlock<TOutput>)@operator).ConsumeMessage(messageHeader, target, out messageConsumed);
    public void Fault(Exception exception)
        => ((IDataflowBlock)@operator).Fault(exception);
    public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        => @operator.LinkTo(target, linkOptions);
    public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)@operator).ReleaseReservation(messageHeader, target);
    public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)@operator).ReserveMessage(messageHeader, target);
}