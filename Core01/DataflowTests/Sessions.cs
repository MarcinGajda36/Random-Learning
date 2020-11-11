using System;
using System.Collections.Generic;
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
                    state.state.Add(1);
                }
                else if (changer is Remover remover)
                {
                    state.state.Remove(1);
                }
            });

            _changer.LinkTo(stateCalculator);
        }

        class State
        {
            public HashSet<int> state = new HashSet<int>();
        }
        class Changer
        {

        }
        class Adder : Changer
        {

        }
        class Remover : Changer
        {

        }
    }
}
