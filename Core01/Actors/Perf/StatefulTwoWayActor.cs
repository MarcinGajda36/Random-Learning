using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Actors.Perf;

public sealed class StatefulTwoWayActor<TState, TInput, TOutput, TOperation>
    : IPropagatorBlock<TInput, TOutput>
    where TOperation : struct, IOperationWithOutput<TState, TInput, TOutput>
{
    private readonly TransformBlock<TInput, TOutput> @operator;
    private TState state;

    public Task Completion => @operator.Completion;

    public StatefulTwoWayActor(TState startingState)
    {
        state = startingState;
        @operator = CreateOperator();
    }

    private TransformBlock<TInput, TOutput> CreateOperator()
        => new(input =>
        {
            (state, var output) = default(TOperation).Execute(state, input);
            return output;
        });

    public bool Post(TInput input)
        => @operator.Post(input);

    public void Complete()
        => @operator.Complete();
    public TOutput? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        => ((ISourceBlock<TOutput>)@operator).ConsumeMessage(messageHeader, target, out messageConsumed);
    public void Fault(Exception exception)
        => ((IDataflowBlock)@operator).Fault(exception);
    public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        => @operator.LinkTo(target, linkOptions);
    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
        => ((ITargetBlock<TInput>)@operator).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)@operator).ReleaseReservation(messageHeader, target);
    public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        => ((ISourceBlock<TOutput>)@operator).ReserveMessage(messageHeader, target);

}
