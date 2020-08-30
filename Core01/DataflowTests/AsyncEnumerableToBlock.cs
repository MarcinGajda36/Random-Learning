using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests
{
    public class AsyncEnumerableToBlock
    {
        public static async Task AsyncEnumeratorConsumer(CancellationToken cancellationToken = default)
        {

            while (cancellationToken.IsCancellationRequested is false)
            {
                try
                {
                    await Synch(cancellationToken);
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc);
                }
                finally
                {
                    await Task.Delay(1);
                }
            }
        }
        private static async Task Synch(CancellationToken cancellationToken)
        {
            BatchBlock<int> syncBatch = new BatchBlock<int>(1);
            ActionBlock<int[]> syncAction = new ActionBlock<int[]>(async i =>
            {
                Console.WriteLine("syncAction");
                //exists pairs
                throw null;

            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 });
            _ = syncBatch.LinkTo(syncAction, new DataflowLinkOptions { PropagateCompletion = true });

            await foreach (int i in AsyncEnumerable(cancellationToken))
            {
                bool isMsgRecived = await syncBatch.SendAsync(i);
                if (isMsgRecived is false || syncAction.Completion.Status == TaskStatus.Faulted)
                {
                    break;
                }
            }
            syncBatch.Complete();
            await syncAction.Completion;
        }


        public static async IAsyncEnumerable<int> AsyncEnumerable([EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            int i = 0;
            while (cancellationToken.IsCancellationRequested is false)
            {
                await Task.Delay(1);
                yield return i++;
            }
        }

    }
}
