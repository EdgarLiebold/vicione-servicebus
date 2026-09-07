using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Creates shared and operation-scoped Amazon connection contexts.</summary>
public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    /// <summary>Initializes a connection-context factory.</summary>
    /// <param name="hostConfiguration">The host configuration used to create and retry connections.</param>
    public ConnectionContextFactory(IAmazonSqsHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;
    }

    /// <summary>Starts asynchronous connection creation under a supervisor.</summary>
    /// <param name="supervisor">The supervisor that owns the connection context.</param>
    /// <returns>The new connection-context agent.</returns>
    public IPipeContextAgent<ConnectionContext> CreateContext(ISupervisor supervisor)
    {
        Task<ConnectionContext> context = Task.Run(() => CreateConnectionAsync(supervisor), supervisor.Stopped);

        IPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddContext(context);

        return contextHandle;
    }

    /// <summary>Creates an operation-scoped connection context from a shared context handle.</summary>
    /// <param name="supervisor">The supervisor that owns the active context.</param>
    /// <param name="context">The shared connection-context handle.</param>
    /// <param name="cancellationToken">The operation cancellation token assigned to the scoped context.</param>
    /// <returns>The active scoped-context agent.</returns>
    public IActivePipeContextAgent<ConnectionContext> CreateActiveContext(ISupervisor supervisor,
        IPipeContextHandle<ConnectionContext> context, CancellationToken cancellationToken)
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
