using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests;

public class BroadcastBlockTest
{
    private static readonly BroadcastBlock<int> bb = new BroadcastBlock<int>(x => x);
    private static int sum;
    public static async Task Test()
    {
        bb
            .AsObservable()
            .Subscribe(i =>
                Interlocked.Add(ref sum, i));
        bb
            .AsObservable()
            .Subscribe(i =>
                Interlocked.Add(ref sum, i));

        bb.Post(1);
        await Task.Delay(100);
        bb.Complete();
        await bb.Completion;
    }
}
