namespace ViciOne.ServiceBus.RabbitMqTransport;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agents;
using Configuration;
using Internals;
using Transports;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;


public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly Lazy<ConnectionFactory> _connectionFactory;
    readonly IRabbitMqHostConfiguration _hostConfiguration;

    public ConnectionContextFactory(IRabbitMqHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;

        _connectionFactory = new Lazy<ConnectionFactory>(() => _hostConfiguration.Settings.GetConnectionFactory());
    }

    public IPipeContextAgent<ConnectionContext> CreateContext(ISupervisor supervisor)
    {
        Task<ConnectionContext> context = Task.Run(() => CreateConnection(supervisor), supervisor.Stopped);

        IPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddContext(context);

        Task HandleShutdown(object sender, ShutdownEventArgs args)
        {
            // Invalidate before stopping, and never dispose from inside this notification: an operation
            // that is still unwinding — a channel creation, say — has to finish touching the connection
            // before the connection goes away. Stopping stays off this thread for the same reason.
            if (context.Status == TaskStatus.RanToCompletion && context.Result is RabbitMqConnectionContext connectionContext)
                connectionContext.Lifetime.Invalidate(args);

            Task.Run(() => contextHandle.Stop(args.ReplyText))
                .IgnoreUnobservedExceptions();

            return Task.CompletedTask;
        }

        context.ContinueWith(task =>
        {
            var connectionContext = task.Result;

            connectionContext.Connection.ConnectionShutdownAsync += HandleShutdown;

            void RemoveHandler(Task _)
            {
                try
                {
                    connectionContext.Connection.ConnectionShutdownAsync -= HandleShutdown;
                }
                catch (ObjectDisposedException)
                {
                }
            }

            contextHandle.Completed.ContinueWith(RemoveHandler);
        }, TaskContinuationOptions.OnlyOnRanToCompletion);

        return contextHandle;
    }

    public IActivePipeContextAgent<ConnectionContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<ConnectionContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnection(context.Context, cancellationToken));
    }

    static async Task<ConnectionContext> CreateSharedConnection(Task<ConnectionContext> contextTask, CancellationToken cancellationToken)
    {
        var context = contextTask.Status == TaskStatus.RanToCompletion
            ? contextTask.Result
            : await contextTask.OrCanceled(cancellationToken).ConfigureAwait(false);

        if (!context.Connection.IsOpen)
        {
            // The connection's own reason, not a locally invented one claiming the peer said this.
            var reason = context.Connection.CloseReason;

            throw new OperationInterruptedException(reason
                ?? new ShutdownEventArgs(ShutdownInitiator.Library, 491, "The connection is no longer available"));
        }

        return new SharedConnectionContext(context, cancellationToken);
    }

    async Task<ConnectionContext> CreateConnection(ISupervisor supervisor)
    {
        await _hostConfiguration.Settings.Refresh(_connectionFactory.Value).ConfigureAwait(false);

        var description = _hostConfiguration.Settings.ToDescription(_connectionFactory.Value);

        if (supervisor.Stopping.IsCancellationRequested)
            throw RabbitMqConnectionException.Stopping(description);

        IConnection connection = null;
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
                List<string> hostNames = [_hostConfiguration.Settings.Host];

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
