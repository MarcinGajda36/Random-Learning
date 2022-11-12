using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Actors.Perf;

public sealed class StatefulOneWayActor<TState, TInput, TOperation>
    : ITargetBlock<TInput>
    where TOperation : struct, IOperationWithoutOutput<TState, TInput>
{
    private readonly ActionBlock<TInput> @operator;
    private TState state;

    public Task Completion => @operator.Completion;

    public StatefulOneWayActor(TState startingState)
    {
        state = startingState;
        @operator = CreateOperator();
    }

    private ActionBlock<TInput> CreateOperator()
        => new(input => state = default(TOperation).Execute(state, input));

    public bool Post(TInput input)
        => @operator.Post(input);

    public void Complete()
        => @operator.Complete();
    public void Fault(Exception exception)
        => ((IDataflowBlock)@operator).Fault(exception);
    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
        => ((ITargetBlock<TInput>)@operator).OfferMessage(messageHeader, messageValue, source, consumeToAccept);

