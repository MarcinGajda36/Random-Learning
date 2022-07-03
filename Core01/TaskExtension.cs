using System;
using System.Threading.Tasks;

namespace MarcinGajda
{
    public static class TaskExtension
    {
        public static async Task<TR> Map<T, TR>(this Task<T> taskT, Func<T, TR> mapper,
            bool continueOnCapturedContext = false)
            => mapper(await taskT.ConfigureAwait(continueOnCapturedContext));

        public static async Task<TR> Bind<T, TR>(this Task<T> taskT, Func<T, Task<TR>> mapper,
            bool continueOnCapturedContext = false)
            => await mapper(await taskT.ConfigureAwait(continueOnCapturedContext)).ConfigureAwait(continueOnCapturedContext);

    }
}
