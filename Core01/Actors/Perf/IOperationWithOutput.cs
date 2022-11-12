namespace MarcinGajda.Actors.Perf;
public interface IOperationWithOutput<TState, TInput, TOutput>
{
    (TState, TOutput) Execute(TState state, TInput input);
}
