using System.Numerics;
using System.Runtime.Intrinsics;

namespace DITesting.Background;

public delegate DateTimeOffset UtcNow();
public delegate bool DateValidatior<T>(DateTimeOffset date, T toValidate);
public delegate bool UtcNow2(UtcNow utcNow);

public class BackgroundSomething : BackgroundService
{
    public BackgroundSomething()
    {
        var xs = new Vector<int>(1);
        var vec = Vector128.Create(1);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => throw new NotImplementedException();
}
