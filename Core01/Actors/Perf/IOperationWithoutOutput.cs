namespace MarcinGajda.Actors.Perf;
public interface IOperationWithOutput<TState, TInput>
{
    TState Execute(TState state, TInput input);
}
