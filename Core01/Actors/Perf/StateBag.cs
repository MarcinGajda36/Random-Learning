namespace MarcinGajda.Actors.Perf;
internal class StateBag<TState>
{
    public TState State { get; set; }

    public StateBag(TState state)
        => State = state;
}
