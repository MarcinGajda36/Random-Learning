using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests
{
    class Sessions
    {
        private readonly BroadcastBlock<Changer> _changer;
        private readonly TransformBlock<Changer, Remover> _removers;
        private readonly TransformBlock<Changer, int> _stateCalculator;
        private readonly BroadcastBlock<int> _states;
        private readonly ActionBlock<int> _stateUpdater;

        public ISourceBlock<int> States => _states;

        public int StateSummary { get; private set; }

        public Sessions()
        {
            _changer = new BroadcastBlock<Changer>(__ => __);
            _removers = new TransformBlock<Changer, Remover>(async changer =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return new Remover();
            }, new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                EnsureOrdered = false
            });
            _ = _changer.LinkTo(_removers, changer => changer is Adder);

            var state = new State();
            _stateCalculator = new TransformBlock<Changer, int>(changer =>
            {
                if (changer is Adder adder)
                {
                    state.state += 1;
                    adder.Result.SetResult(state.state);
                }
                else if (changer is Remover remover)
                {
                    state.state -= 1;
                }
                return state.state;
            });
            _ = _changer.LinkTo(_stateCalculator);
            _ = _removers.LinkTo(_stateCalculator);

            _states = new BroadcastBlock<int>(__ => __);
            _ = _stateCalculator.LinkTo(_states);

            _stateUpdater = new ActionBlock<int>(state => StateSummary = state);
            _ = _states.LinkTo(_stateUpdater);
        }
        public Task<int> Add()
        {
            var adder = new Adder();
            _changer.Post(adder);
            return adder.Result.Task;
        }
        class State
        {
            public int state = 0;
        }
        class Changer
        {

        }
        class Adder : Changer
        {
            public TaskCompletionSource<int> Result = new TaskCompletionSource<int>();
        }
        class Remover : Changer
        {

        }
    }
}
