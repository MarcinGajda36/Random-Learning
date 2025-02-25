using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests;

public class MyQueue
{
    private readonly BufferBlock<int> bufferBlock =
        new BufferBlock<int>(new DataflowBlockOptions
        {
            BoundedCapacity = DataflowBlockOptions.Unbounded,
            CancellationToken = CancellationToken.None,
            EnsureOrdered = true,
        });

    public ISourceBlock<int> SourceBlock => bufferBlock;

    public Task<bool> Enqueue(int toEnqueue) => bufferBlock.SendAsync(toEnqueue);

}
