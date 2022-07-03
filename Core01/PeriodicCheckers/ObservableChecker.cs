using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.PeriodicCheckers
{
    public class ObservableChecker
    {
        public static async Task TestBasic1()
        {
            using Timer timer = new Timer(async (state) =>
            {
                Console.WriteLine("before");
                await Task.Delay(1000);
                Console.WriteLine("After");
            }, "state", 1, 100);

            await Task.Delay(-1);

            var allResults = Observable.Interval(TimeSpan.FromSeconds(10))
                .Select(i => Task.WhenAll((new[] { i }).Select(Task.FromResult)))
                .Do(async resullts => Array.ForEach(await resullts, Console.WriteLine));


            var lastResults = await allResults;
        }
    }
}
