using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests
{
    class Sessions
    {
        private readonly BroadcastBlock<Changer> _changer = new BroadcastBlock<Changer>(x => x);

        public Sessions()
        {
            var removers = new TransformBlock<Changer, Remover>(async changer =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return new Remover();
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });
            _changer.LinkTo(removers, changer => changer is Adder);
            removers.LinkTo(_changer);

            var state = new State();
            var stateCalculator = new ActionBlock<Changer>(changer =>
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
            });

            _changer.LinkTo(stateCalculator);
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
