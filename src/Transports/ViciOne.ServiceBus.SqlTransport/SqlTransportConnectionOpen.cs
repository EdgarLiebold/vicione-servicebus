using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Transfers an opened connection to its caller or releases it when opening fails.</summary>
internal static class SqlTransportConnectionOpen
{
    public static async Task<TConnection> OpenOwnedAsync<TConnection>(
        TConnection connection,
        Func<TConnection, CancellationToken, Task> openAsync,
        CancellationToken cancellationToken)
        where TConnection : IAsyncDisposable
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(openAsync);

        try
        {
            await openAsync(connection, cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            try
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // An unsuccessful cleanup must not replace the opening failure or cancellation.
            }

            throw;
        }
    }
}
