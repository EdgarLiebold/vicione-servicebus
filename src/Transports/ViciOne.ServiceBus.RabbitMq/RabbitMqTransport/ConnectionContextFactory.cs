using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Creates supervised RabbitMQ connection contexts and invalidates them on broker shutdown.</summary>
public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly Lazy<ConnectionFactory> _connectionFactory;
    readonly IRabbitMqHostConfiguration _hostConfiguration;

    /// <summary>Creates a connection factory from the effective RabbitMQ host configuration.</summary>
    /// <param name="hostConfiguration">The host configuration used for every connection attempt.</param>
    public ConnectionContextFactory(IRabbitMqHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;

        _connectionFactory = new Lazy<ConnectionFactory>(() =>
        {
            ConnectionFactory factory = _hostConfiguration.Settings.GetConnectionFactory();
            if (_hostConfiguration is IMessageLimitsHostConfiguration { MessageLimits: { } limits })
                factory.MaxInboundMessageBodySize = checked((uint)limits.MaxEnvelopeBytes);

            return factory;
        });
    }

    /// <summary>Creates and monitors an owned RabbitMQ connection context.</summary>
    /// <param name="supervisor">The supervisor that owns the context agent.</param>
    /// <returns>The connection-context agent.</returns>
    public IPipeContextAgent<ConnectionContext> CreateContext(ISupervisor supervisor)
    {
        Task<ConnectionContext> context = CreateConnectionAsync(supervisor);

        IPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddContext(context);

        Task HandleShutdownAsync(object sender, ShutdownEventArgs args)
        {
            // Invalidate immediately and defer disposal until every active connection lease has finished.
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

    /// <summary>Creates a scoped view over an existing active connection context.</summary>
    /// <param name="supervisor">The supervisor that owns the scoped view.</param>
    /// <param name="context">The handle for the shared connection context.</param>
    /// <param name="cancellationToken">Cancellation linked to the scoped view.</param>
    /// <returns>The active scoped context agent.</returns>
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
            // Prefer the typed broker reply; synthesize a library reply only when none exists.
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
                    ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "A RabbitMQ host name is required when no endpoint resolver is configured.", "Correct the named configuration before starting the host"));
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
