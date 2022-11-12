using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Actors.Perf;
public interface IOperationWithoutOutput<TState, TInput>
{
    TState Execute(TState state, TInput input);
}

public sealed class StatefulOneWayActor<TState, TInput, TOperation>
    where TOperation : struct, IOperationWithoutOutput<TState, TInput>
{
    private readonly ActionBlock<StateInputBag<TState, TInput>> @operator;
    private readonly StateBag<TState> stateBag;

    public StatefulOneWayActor(TState startingState)
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

