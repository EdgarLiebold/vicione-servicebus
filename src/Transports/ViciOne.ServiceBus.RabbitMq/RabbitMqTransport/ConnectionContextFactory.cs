using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a connection context factory implementation.
/// </summary>
public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly Lazy<ConnectionFactory> _connectionFactory;
    readonly IRabbitMqHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    public ConnectionContextFactory(IRabbitMqHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;

        _connectionFactory = new Lazy<ConnectionFactory>(() => _hostConfiguration.Settings.GetConnectionFactory());
    }

    /// <summary>
    /// Creates context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipeContextAgent<ConnectionContext> CreateContext(ISupervisor supervisor)
    {
        Task<ConnectionContext> context = CreateConnectionAsync(supervisor);

        IPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddContext(context);

        Task HandleShutdownAsync(object sender, ShutdownEventArgs args)
        {
            // Invalidate before stopping, and never dispose from inside this notification: an operation
            // that is still unwinding — a channel creation, say — has to finish touching the connection
            // before the connection goes away. RabbitMQ's callback is already asynchronous, so its
            // returned task is the lifecycle owner and no detached ThreadPool hop is necessary.
            if (context.Status == TaskStatus.RanToCompletion && context.Result is RabbitMqConnectionContext connectionContext)
            {
                connectionContext.TopologyEntityCache.Invalidate();
                connectionContext.Lifetime.Invalidate(args);
            }

            return contextHandle.StopAsync(args.ReplyText);
        }

        context.GetAwaiter().OnCompleted(() =>
        {
            if (!context.IsCompletedSuccessfully)
                return;

            var connectionContext = context.Result;

            connectionContext.Connection.ConnectionShutdownAsync += HandleShutdownAsync;

            void RemoveHandler()
            {
                try
                {
                    connectionContext.Connection.ConnectionShutdownAsync -= HandleShutdownAsync;
                }
                catch (ObjectDisposedException)
                {
                }
            }

            contextHandle.Completed.GetAwaiter().OnCompleted(RemoveHandler);
        });

        return contextHandle;
    }

    /// <summary>
    /// Creates active context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IActivePipeContextAgent<ConnectionContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<ConnectionContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionAsync(context.Context, cancellationToken));
    }

    static async Task<ConnectionContext> CreateSharedConnectionAsync(Task<ConnectionContext> contextTask, CancellationToken cancellationToken)
    {
        var context = contextTask.Status == TaskStatus.RanToCompletion
            ? contextTask.Result
            : await contextTask.OrCanceledAsync(cancellationToken).ConfigureAwait(false);

        if (!context.Connection.IsOpen)
        {
            // The connection's own reason, not a locally invented one claiming the peer said this.
            var reason = context.Connection.CloseReason;

            throw new OperationInterruptedException(reason
                ?? new ShutdownEventArgs(ShutdownInitiator.Library, 491, "The connection is no longer available"));
        }

        return new SharedConnectionContext(context, cancellationToken);
    }

    async Task<ConnectionContext> CreateConnectionAsync(ISupervisor supervisor)
    {
        await _hostConfiguration.Settings.RefreshAsync(_connectionFactory.Value).ConfigureAwait(false);

        var description = _hostConfiguration.Settings.ToDescription(_connectionFactory.Value);

        if (supervisor.Stopping.IsCancellationRequested)
            throw RabbitMqConnectionException.Stopping(description);

        IConnection? connection = null;
        try
        {
            TransportLogMessages.ConnectHost(description);

            if (_hostConfiguration.Settings.EndpointResolver != null)
            {
                connection = await _connectionFactory.Value.CreateConnectionAsync(_hostConfiguration.Settings.EndpointResolver,
                    _hostConfiguration.Settings.ClientProvidedName).ConfigureAwait(false);
            }
            else
            {
                var hostName = _hostConfiguration.Settings.Host
                    ?? throw new ConfigurationException("A RabbitMQ host name is required when no endpoint resolver is configured.");
                List<string> hostNames = [hostName];

                connection = await _connectionFactory.Value.CreateConnectionAsync(hostNames, _hostConfiguration.Settings.ClientProvidedName)
                    .ConfigureAwait(false);
            }

            LogContext.Debug?.Log("Connected: {Host} (address: {RemoteAddress}, local: {LocalAddress})", description, connection.Endpoint,
                connection.LocalPort);

            var connectionContext = new RabbitMqConnectionContext(connection, _hostConfiguration, description, supervisor.Stopped);

            connectionContext.GetOrAddPayload(() => _hostConfiguration.Settings);

            return connectionContext;
        }
        catch (ConnectFailureException ex)
        {
            connection?.Dispose();

            LogContext.Warning?.Log(ex, "Connection Failed: {InputAddress}", _hostConfiguration.HostAddress);

            throw new RabbitMqConnectionException("Connect failed: " + description, ex);
        }
        catch (BrokerUnreachableException ex)
        {
            connection?.Dispose();

            LogContext.Warning?.Log(ex, "Connection Failed: {InputAddress}", _hostConfiguration.HostAddress);

            throw new RabbitMqConnectionException("Broker unreachable: " + description, ex);
        }
        catch (OperationInterruptedException ex)
        {
            connection?.Dispose();

            LogContext.Warning?.Log(ex, "Connection Failed: {InputAddress}", _hostConfiguration.HostAddress);

            throw new RabbitMqConnectionException("Operation interrupted: " + description, ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            connection?.Dispose();

            LogContext.Warning?.Log(ex, "Connection Failed: {InputAddress}", _hostConfiguration.HostAddress);

            throw new RabbitMqConnectionException("Create Connection Faulted: " + description, ex);
        }
    }
}
