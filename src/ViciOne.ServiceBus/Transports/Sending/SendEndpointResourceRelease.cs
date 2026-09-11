using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

internal static class SendEndpointResourceRelease
{
    internal static async Task<IReadOnlyList<Exception>> CollectFailuresAsync(
        ConnectHandle? observerHandle,
        ISendTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);

        var failures = new List<Exception>(2);
        try
        {
            observerHandle?.Disconnect();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        if (transport is IAsyncDisposable disposable)
        {
            try
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        return failures;
    }
}
