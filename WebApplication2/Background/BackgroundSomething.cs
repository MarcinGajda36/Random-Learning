namespace WebApplication2.Background;

public delegate DateTimeOffset UtcNow();
public delegate bool DateValidatior<T>(DateTimeOffset date, T toValidate);
public delegate bool UtcNow2(UtcNow utcNow);

public class BackgroundSomething : BackgroundService
{
    public BackgroundSomething()
    {

    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => throw new NotImplementedException();
}
