namespace MarcinGajda.Actors.Perf;
public interface IOperationWithOutput<TState, TInput, TOutput>
{
    static abstract (TState, TOutput) Execute(TState state, TInput input);
}
