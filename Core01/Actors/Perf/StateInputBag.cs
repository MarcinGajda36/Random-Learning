namespace MarcinGajda.Actors.Perf;
internal struct StateInputBag<TState, TInput>
{
    public StateBag<TState> StateBag { get; }
    public TInput Input { get; }

    public StateInputBag(StateBag<TState> stateBag, TInput input)
    {
        StateBag = stateBag;
        Input = input;
    }
}
