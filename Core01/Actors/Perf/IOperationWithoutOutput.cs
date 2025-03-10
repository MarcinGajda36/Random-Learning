namespace MarcinGajda.Actors.Perf;
public interface IOperationWithoutOutput<TState, TInput>
{
    static abstract TState Execute(TState state, TInput input);
}
