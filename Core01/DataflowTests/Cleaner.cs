using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests;

public class Cleaner
{

    private readonly ActionBlock<(string Path, CancellationToken CancellationToken)> actionBlock =
        new ActionBlock<(string Path, CancellationToken CancellationToken)>(pathCancelPair =>
        {
            if (pathCancelPair.CancellationToken.IsCancellationRequested)
            {
                return;
            }
        });

    private readonly SerialDisposable serialDisp = new SerialDisposable();
    private void WebSucker()
    {
        var cancelSource = new CancellationTokenSource();
        serialDisp.Disposable = cancelSource;
    }
}
