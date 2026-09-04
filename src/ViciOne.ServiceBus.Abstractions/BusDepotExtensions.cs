using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public static class BusDepotExtensions
{
    public static async Task StartAsync(this IBusDepot depot, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var timeoutTokenSource = new CancellationTokenSource(timeout);

        if (cancellationToken.CanBeCanceled)
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);

            await depot.StartAsync(linkedTokenSource.Token).ConfigureAwait(false);
        }
        else
            await depot.StartAsync(timeoutTokenSource.Token).ConfigureAwait(false);
    }

    public static async Task StopAsync(this IBusDepot depot, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var timeoutTokenSource = new CancellationTokenSource(timeout);

        if (cancellationToken.CanBeCanceled)
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);

            await depot.StopAsync(linkedTokenSource.Token).ConfigureAwait(false);
        }
        else
            await depot.StopAsync(timeoutTokenSource.Token).ConfigureAwait(false);
    }
}
