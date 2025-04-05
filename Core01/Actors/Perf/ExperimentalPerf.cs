//namespace MarcinGajda.Actors.Perf;
//using System;
//using System.Threading.Tasks;
//using System.Threading.Tasks.Dataflow;

//internal sealed class StateBox<TState>
//{
//    public TState State { get; set; }

//    public StateBox(TState state)
//        => State = state;
//}

//internal readonly record struct StateInputBox<TState, TInput>(StateBox<TState> StateBag, TInput Input);

//public sealed class ExperimentalStatefulOneWayBlock<TState, TInput, TOperation>
//    //: ITargetBlock<TInput>
//    where TOperation : struct, IOperationWithoutOutput<TState, TInput>
//{
//    private readonly ActionBlock<StateInputBox<TState, TInput>> @operator;
//    private readonly StateBox<TState> state;

//    public ExperimentalStatefulOneWayBlock(TState startingState)
//    {
//        state = new(startingState);
//        @operator = CreateOperator();
//    }

//    public Task Completion => @operator.Completion;

//    private static ActionBlock<StateInputBox<TState, TInput>> CreateOperator()
//        => new(static inputBag
//            => inputBag.StateBag.State = default(TOperation).Execute(inputBag.StateBag.State, inputBag.Input));

//    public bool Post(TInput input)
//        => @operator.Post(new(state, input));

//    public void Complete()
//        => @operator.Complete();

//    public void Fault(Exception exception)
//        => ((IDataflowBlock)@operator).Fault(exception);
//}

//public sealed class ExperimentalStatefulTwoWayBlock<TState, TInput, TOutput, TOperation>
//    : ISourceBlock<TOutput>
//    //: IPropagatorBlock<TInput, TOutput>
//    where TOperation : struct, IOperationWithOutput<TState, TInput, TOutput>
//{
//    private readonly TransformBlock<StateInputBox<TState, TInput>, TOutput> @operator;
//    private readonly StateBox<TState> state;

//    public ExperimentalStatefulTwoWayBlock(TState startingState)
//    {
//        state = new(startingState);
//        @operator = CreateOperator();
//    }

//    public Task Completion => @operator.Completion;

//    private static TransformBlock<StateInputBox<TState, TInput>, TOutput> CreateOperator()
//        => new(static inputBag =>
//        {
//            (inputBag.StateBag.State, var output) = default(TOperation).Execute(inputBag.StateBag.State, inputBag.Input);
//            return output;
//        });

//    public bool Post(TInput input)
//        => @operator.Post(new(state, input));

//    public void Complete()
//        => @operator.Complete();

//    public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
//        => @operator.LinkTo(target, linkOptions);

//    TOutput? ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
//        => ((ISourceBlock<TOutput>)@operator).ConsumeMessage(messageHeader, target, out messageConsumed);
//    void IDataflowBlock.Fault(Exception exception)
//        => ((IDataflowBlock)@operator).Fault(exception);
//    void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
//        => ((ISourceBlock<TOutput>)@operator).ReleaseReservation(messageHeader, target);
//    bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
//        => ((ISourceBlock<TOutput>)@operator).ReserveMessage(messageHeader, target);
//}