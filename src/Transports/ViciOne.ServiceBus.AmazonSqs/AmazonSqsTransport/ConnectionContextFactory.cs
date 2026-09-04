using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a connection context factory implementation.
/// </summary>
public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    public ConnectionContextFactory(IAmazonSqsHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;
    }

    /// <summary>
    /// Creates context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipeContextAgent<ConnectionContext> CreateContext(ISupervisor supervisor)
    {
        Task<ConnectionContext> context = Task.Run(() => CreateConnectionAsync(supervisor), supervisor.Stopped);

        IPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddContext(context);

        return contextHandle;
    }

    /// <summary>
    /// Creates active context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IActivePipeContextAgent<ConnectionContext> CreateActiveContext(ISupervisor supervisor,
        PipeContextHandle<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionAsync(context.Context, cancellationToken));
    }

    static async Task<ConnectionContext> CreateSharedConnectionAsync(Task<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedConnectionContext(context.Result, cancellationToken)
            : new SharedConnectionContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    async Task<ConnectionContext> CreateConnectionAsync(ISupervisor supervisor)
    {
        return await _hostConfiguration.ReceiveTransportRetryPolicy.RetryAsync(async () =>
        {
            if (supervisor.Stopping.IsCancellationRequested)
                throw new AmazonSqsConnectionException($"The connection is stopping and cannot be used: {_hostConfiguration.HostAddress}");

            try
            {
                TransportLogMessages.ConnectHost(_hostConfiguration.Settings.ToString());

                var connection = _hostConfiguration.Settings.CreateConnection();

                return new AmazonSqsConnectionContext(connection, _hostConfiguration, supervisor.Stopped);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogContext.Warning?.Log(ex, "Connection Failed: {InputAddress}", _hostConfiguration.HostAddress);
                throw new AmazonSqsConnectionException("Connect failed: " + _hostConfiguration.Settings, ex);
            }
        }, supervisor.Stopping).ConfigureAwait(false);
    }
}
