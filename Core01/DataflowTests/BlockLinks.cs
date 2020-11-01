using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests
{
    class BlockLinks
    {
        public void Test()
        {
            var action = new ActionBlock<int[]>(x => { }, new ExecutionDataflowBlockOptions { });
            var batch = new BatchBlock<int>(10, new GroupingDataflowBlockOptions { });
            using var likBA = batch.LinkTo(action, new DataflowLinkOptions { }, x => x[0] > 1);//actionBlock gets 10 elements
            batch.TriggerBatch();//for leftover elements 

            var batchJoined = new BatchedJoinBlock<int[], string>(10, new GroupingDataflowBlockOptions { });
            batch.LinkTo(batchJoined.Target1, new DataflowLinkOptions { }, ints => ints[0] > 0);
            var action2 = new ActionBlock<Tuple<IList<int[]>, IList<string>>>(x => { });
            using var linkBJA = batchJoined.LinkTo(action2, new DataflowLinkOptions { }, x => x.Item1.Count == x.Item2.Count);//does this always recive 10-10 or any 10 total (like 7 -3) trigger link?

            var broadcast = new BroadcastBlock<int>(cloningFunction: x => x, new DataflowBlockOptions { });
            var action3 = new ActionBlock<int>(x => { });
            var action4 = new ActionBlock<int>(x => { });
            broadcast.LinkTo(action3, new DataflowLinkOptions { }, x => x > 0);
            broadcast.LinkTo(action4, new DataflowLinkOptions { }, x => x > 0);//both will gete same elements 

            var buffer = new BufferBlock<int>(new DataflowBlockOptions { });
            var action5 = new ActionBlock<int>(x => { });
            buffer.LinkTo(action5, new DataflowLinkOptions { }, x => x == 0);

            var buffer1 = new BufferBlock<int>();
            var join = new JoinBlock<int, string>(new GroupingDataflowBlockOptions { });
            buffer1.LinkTo(join.Target1);
            var action6 = new ActionBlock<Tuple<int, string>>(tlp => { });
            join.LinkTo(action6);

            var transform = new TransformBlock<int, string>(x => x.ToString(), new ExecutionDataflowBlockOptions { });
            var action7 = new ActionBlock<string>(str => { });
            transform.LinkTo(action7);

            var transformMany = new TransformManyBlock<int, string>(
                x => Enumerable.Range(0, x).Select(i => i.ToString()));//one recived to many output
            var action8 = new ActionBlock<string>(str => { });

            var writeOnce = new WriteOnceBlock<int>(cloningFunction: x => x, new DataflowBlockOptions { });// it gets and stores 1 element but gives clones to linked blocks? 
            var action9 = new ActionBlock<int>(x => { });
            writeOnce.LinkTo(action9);
        }
    }
}
