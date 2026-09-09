using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

internal static class PipeContextDisposer
{
    public static async Task DisposeAsync(PipeContext context)
    {
        switch (context)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }
}
