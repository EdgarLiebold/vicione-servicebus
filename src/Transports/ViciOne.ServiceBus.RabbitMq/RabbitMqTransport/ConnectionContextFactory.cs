using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Creates supervised RabbitMQ connection contexts and invalidates them on broker shutdown.</summary>
public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly Func<ConnectionFactory, RabbitMqHostSettings, CancellationToken, Task<IConnection>> _connect;
    readonly Lazy<ConnectionFactory> _connectionFactory;
    readonly IRabbitMqHostConfiguration _hostConfiguration;

    /// <summary>Creates a connection factory from the effective RabbitMQ host configuration.</summary>
    /// <param name="hostConfiguration">The host configuration used for every connection attempt.</param>
    public ConnectionContextFactory(IRabbitMqHostConfiguration hostConfiguration)
        : this(hostConfiguration, ConnectAsync)
    {
    }

    internal ConnectionContextFactory(IRabbitMqHostConfiguration hostConfiguration,
        Func<ConnectionFactory, RabbitMqHostSettings, CancellationToken, Task<IConnection>> connect)
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        _connect = connect ?? throw new ArgumentNullException(nameof(connect));

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
        IAsyncPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddAsyncContext<ConnectionContext>();
        RabbitMqConnectionContext? connectionContext = null;

        Task HandleShutdownAsync(object sender, ShutdownEventArgs args)
        {
            if (connectionContext is { } currentContext)
            {
                currentContext.TopologyEntityCache.Invalidate();
                currentContext.Lifetime.Invalidate(args);
            }

            // Application shutdown is raised by the connection disposal already in progress.
            // Re-entering the same context stop here would make CloseAsync wait for itself.
            if (args.Initiator == ShutdownInitiator.Application && contextHandle.Context.IsCompletedSuccessfully)
                return Task.CompletedTask;

            return contextHandle.StopAsync(args.ReplyText);
        }

        async Task CreateAndPublishConnectionAsync()
        {
            RabbitMqConnectionContext? created = null;
            try
            {
                created = await CreateConnectionAsync(supervisor).ConfigureAwait(false);
                RabbitMqConnectionContext registered = created;
                connectionContext = registered;
                registered.Connection.ConnectionShutdownAsync += HandleShutdownAsync;

                void RemoveHandler()
                {
                    try
                    {
                        registered.Connection.ConnectionShutdownAsync -= HandleShutdownAsync;
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (Exception exception)
                    {
                        try
                        {
                            LogContext.Error?.Log(exception,
                                "Removing the RabbitMQ connection shutdown handler faulted after context completion");
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                contextHandle.Completed.GetAwaiter().OnCompleted(RemoveHandler);

                if (!registered.Connection.IsOpen)
                {
                    ShutdownEventArgs reason = registered.Connection.CloseReason
                        ?? new ShutdownEventArgs(ShutdownInitiator.Library, 491, "The connection is no longer available");
                    await HandleShutdownAsync(registered.Connection, reason).ConfigureAwait(false);
                }

                RabbitMqConnectionContext published = created;
                created = null;
                await contextHandle.CreatedAsync(published).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                if (created is not null)
                {
                    await DisposeUnpublishedConnectionAsync(created).ConfigureAwait(false);
                    created = null;
                }

                CancellationToken cancellationToken = exception.CancellationToken.CanBeCanceled
                    ? exception.CancellationToken
                    : supervisor.Stopping;
                await contextHandle.CreateCanceledAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (created is not null)
                {
                    await DisposeUnpublishedConnectionAsync(created).ConfigureAwait(false);
                    created = null;
                }

                await contextHandle.CreateFaultedAsync(exception).ConfigureAwait(false);
            }
        }

        CreateAndPublishConnectionAsync().IgnoreUnobservedExceptions();

        return contextHandle;
    }

    /// <summary>Creates a scoped view over an existing active connection context.</summary>
    /// <param name="supervisor">The supervisor that owns the scoped view.</param>
    /// <param name="context">The handle for the shared connection context.</param>
    /// <param name="cancellationToken">Cancellation linked to the scoped view.</param>
    /// <returns>The active scoped context agent.</returns>
    public IActivePipeContextAgent<ConnectionContext> CreateActiveContext(ISupervisor supervisor, IPipeContextHandle<ConnectionContext> context,
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

    async Task<RabbitMqConnectionContext> CreateConnectionAsync(ISupervisor supervisor)
    {
        RabbitMqHostSettings settings = _hostConfiguration.Settings;
        ConnectionFactory connectionFactory = _connectionFactory.Value;
        var description = settings.ToDescription(connectionFactory);

        if (supervisor.Stopping.IsCancellationRequested)
            throw RabbitMqConnectionException.Stopping(description);

        IConnection? connection = null;
        try
        {
            await settings.RefreshAsync(connectionFactory, supervisor.Stopping).ConfigureAwait(false);
            description = settings.ToDescription(connectionFactory);

            if (supervisor.Stopping.IsCancellationRequested)
                throw RabbitMqConnectionException.Stopping(description);

            TransportLogMessages.ConnectHost(description);

            connection = await _connect(connectionFactory, settings, supervisor.Stopping).ConfigureAwait(false);

            LogContext.Debug?.Log("Connected: {Host} (address: {RemoteAddress}, local: {LocalAddress})", description, connection.Endpoint,
                connection.LocalPort);

            var connectionContext = new RabbitMqConnectionContext(connection, _hostConfiguration, description, supervisor.Stopped);

            connectionContext.GetOrAddPayload(() => settings);

            return connectionContext;
        }
        catch (Exception ex)
        {
            DisposeConnection(connection);

            if (ex is ConfigurationException or OperationCanceledException or RabbitMqConnectionException)
                throw;

            LogConnectionFailure(ex, _hostConfiguration.HostAddress);

            string message = ex switch
            {
                ConnectFailureException => "Connect failed: ",
                BrokerUnreachableException => "Broker unreachable: ",
                OperationInterruptedException => "Operation interrupted: ",
                _ => "Create Connection Faulted: ",
            };

            throw new RabbitMqConnectionException(message + description, ex);
        }
    }

    static void DisposeConnection(IConnection? connection)
    {
        try
        {
            connection?.Dispose();
        }
        catch (Exception exception)
        {
            try
            {
                LogContext.Error?.Log(exception, "Disposing a failed RabbitMQ connection faulted; the primary failure is preserved");
            }
            catch (Exception)
            {
            }
        }
    }

    static async Task DisposeUnpublishedConnectionAsync(RabbitMqConnectionContext connectionContext)
    {
        try
        {
            await connectionContext.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            try
            {
                LogContext.Error?.Log(exception, "Disposing an unpublished RabbitMQ connection context faulted");
            }
            catch (Exception)
            {
            }
        }
    }

    static void LogConnectionFailure(Exception exception, Uri inputAddress)
    {
        try
        {
            LogContext.Warning?.Log(exception, "Connection Failed: {InputAddress}", inputAddress);
        }
        catch (Exception)
        {
        }
    }

    static Task<IConnection> ConnectAsync(ConnectionFactory connectionFactory, RabbitMqHostSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.EndpointResolver != null)
            return connectionFactory.CreateConnectionAsync(settings.EndpointResolver, settings.ClientProvidedName, cancellationToken);

        var hostName = settings.Host
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "A RabbitMQ host name is required when no endpoint resolver is configured.", "Correct the named configuration before starting the host"));
        List<string> hostNames = [hostName];

        return connectionFactory.CreateConnectionAsync(hostNames, settings.ClientProvidedName, cancellationToken);
    }
}
