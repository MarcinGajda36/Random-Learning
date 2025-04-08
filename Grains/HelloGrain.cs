namespace Grains;
using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.Extensions.Logging;

public class HelloGrain(ILogger<HelloGrain> logger)
    : Grain, IHello
{
    public ValueTask<string> SayHello(string greeting)
    {
        logger.LogInformation(
            """
            SayHello message received: greeting = "{Greeting}"
            """,
            greeting);
        return ValueTask.FromResult(
            $"""
            Client said: "{greeting}", so {nameof(HelloGrain)} says: Hello!
            """);
    }
}
