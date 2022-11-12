namespace MarcinGajda.Actors.Perf;
public interface IOperationWithoutOutput<TState, TInput>
{
    TState Execute(TState state, TInput input);
}
