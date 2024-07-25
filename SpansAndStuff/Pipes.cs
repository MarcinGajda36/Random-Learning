using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpansAndStuff;
internal class Pipes
{
    public static async Task Test()
    {
        var pipe = new Pipe(new PipeOptions() { });
        PipeReader reader = pipe.Reader;
        PipeWriter writer = pipe.Writer;
        JsonSerializer.Serialize<object>(new { }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
    }
}
