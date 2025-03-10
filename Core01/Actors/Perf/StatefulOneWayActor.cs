namespace MarcinGajda.Actors.Perf;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public sealed class StatefulOneWayActor<TState, TInput, TOperation>
    : ITargetBlock<TInput>
    where TOperation : IOperationWithoutOutput<TState, TInput>
{
    private readonly ActionBlock<TInput> @operator;
    public TState State { get; private set; }

    public Task Completion
        => @operator.Completion;

    public StatefulOneWayActor(TState startingState)
    {
        State = startingState;
        @operator = CreateOperator();
    }

    private ActionBlock<TInput> CreateOperator()
        => new(input => State = TOperation.Execute(State, input));

    public bool Post(TInput input)
        => @operator.Post(input);

    public void Complete()
        => @operator.Complete();
    public void Fault(Exception exception)
        => ((IDataflowBlock)@operator).Fault(exception);
    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
        => ((ITargetBlock<TInput>)@operator).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
}

