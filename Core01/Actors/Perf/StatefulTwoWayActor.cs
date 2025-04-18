﻿namespace MarcinGajda.Actors.Perf;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public sealed class StatefulTwoWayActor<TState, TInput, TOutput, TOperation>
    : IPropagatorBlock<TInput, TOutput>
    where TOperation : IOperationWithOutput<TState, TInput, TOutput>
{
    private readonly TransformBlock<TInput, TOutput> @operator;
    public TState State { get; private set; }

    public Task Completion
        => @operator.Completion;

    public StatefulTwoWayActor(TState startingState)
    {
        State = startingState;
        @operator = CreateOperator();
    }

    private TransformBlock<TInput, TOutput> CreateOperator()
        => new(input =>
        {
            (State, var output) = TOperation.Execute(State, input);
            return output;
        });

    public bool Post(TInput input)
        => @operator.Post(input);

    public void Complete()
        => @operator.Complete();

    public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        => @operator.LinkTo(target, linkOptions);

    TOutput? ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        => ((ISourceBlock<TOutput>)@operator).ConsumeMessage(messageHeader, target, out messageConsumed);
    void IDataflowBlock.Fault(Exception exception)
        => ((IDataflowBlock)@operator).Fault(exception);
    DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
        => ((ITargetBlock<TInput>)@operator).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)@operator).ReleaseReservation(messageHeader, target);
    bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)@operator).ReserveMessage(messageHeader, target);
}

public sealed class StatefulTwoWayActor<TState, TInput, TOutput>(
    TState startingState,
    Func<TState, TInput, (TState, TOutput)> operation)
    : IPropagatorBlock<TInput, TOutput>
{
    private readonly struct FuncInStateOperation
        : IOperationWithOutput<(TState, Func<TState, TInput, (TState, TOutput)>), TInput, TOutput>
    {
        public static ((TState, Func<TState, TInput, (TState, TOutput)>), TOutput) Execute(
            (TState, Func<TState, TInput, (TState, TOutput)>) state,
            TInput input)
        {
            var (oldState, func) = state;
            var (newState, output) = func(oldState, input);
            return ((newState, func), output);
        }
    }

    private readonly StatefulTwoWayActor<(TState, Func<TState, TInput, (TState, TOutput)>), TInput, TOutput, FuncInStateOperation> @operator
        = new((startingState, operation));

    public Task Completion
        => @operator.Completion;

    public bool Post(TInput input)
        => @operator.Post(input);

    public void Complete()
        => @operator.Complete();

    public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        => @operator.LinkTo(target, linkOptions);

    TOutput? ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        => ((ISourceBlock<TOutput>)@operator).ConsumeMessage(messageHeader, target, out messageConsumed);
    void IDataflowBlock.Fault(Exception exception)
        => ((IDataflowBlock)@operator).Fault(exception);
    DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
        => ((ITargetBlock<TInput>)@operator).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)@operator).ReleaseReservation(messageHeader, target);
    bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)@operator).ReserveMessage(messageHeader, target);
}

public static class StatefulTwoWayActor
{
    public static StatefulTwoWayActor<TState, TInput, TOutput> Create<TState, TInput, TOutput>(
        TState startingState,
        Func<TState, TInput, (TState, TOutput)> operation)
        => new(startingState, operation);
}
