using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Creates, monitors, and shares ActiveMQ connection contexts for a supervisor.</summary>
public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly IActiveMqHostConfiguration _hostConfiguration;

    /// <summary>Creates a connection-context factory for an ActiveMQ host.</summary>
    /// <param name="hostConfiguration">The ActiveMQ host configuration.</param>
    public ConnectionContextFactory(IActiveMqHostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);
        _hostConfiguration = hostConfiguration;
    }

    IPipeContextAgent<ConnectionContext> IPipeContextFactory<ConnectionContext>.CreateContext(ISupervisor supervisor)
    {
        Task<ConnectionContext> context = Task.Run(() => CreateConnection(supervisor), supervisor.Stopped);

        IPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddContext(context);

        var faultStopLock = new object();
        Task? faultStopTask = null;

        void HandleConnectionException(Exception exception)
        {
            TaskCompletionSource stopCompletion;
            lock (faultStopLock)
            {
                if (faultStopTask == null || faultStopTask.IsCompleted)
                {
                    stopCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    faultStopTask = stopCompletion.Task;
                }
                else
                    return;
            }

            // Claim the transition before Stop is invoked, because NMS can report another
            // exception synchronously while that stop tears the connection down.
            StopAfterConnectionExceptionAsync(exception, stopCompletion).IgnoreUnobservedExceptions();
        }

        async Task StopAfterConnectionExceptionAsync(Exception exception, TaskCompletionSource stopCompletion)
        {
            try
            {
                await contextHandle.StopAsync($"Connection Exception: {exception}").ConfigureAwait(false);
            }
            catch (Exception stopException)
            {
                LogLifecycleFailure(stopException, "Stopping faulted ActiveMQ connection context failed");
            }
            finally
            {
                stopCompletion.TrySetResult();
            }
        }

        MonitorConnectionAsync().IgnoreUnobservedExceptions();

        async Task MonitorConnectionAsync()
        {
            ConnectionContext connectionContext;
            try
            {
                connectionContext = await context.ConfigureAwait(false);
            }
            catch
            {
                // The context task owns startup failure; no listener was acquired.
                return;
            }

            int listenerRemoved = 0;
            void RemoveListener()
            {
                if (Interlocked.Exchange(ref listenerRemoved, 1) != 0)
                    return;
                try
                {
                    connectionContext.Connection.ExceptionListener -= HandleConnectionException;
                }
                catch (Exception exception)
                {
                    LogLifecycleFailure(exception, "Removing ActiveMQ connection exception listener failed");
                }
            }

            try
            {
                connectionContext.Connection.ExceptionListener += HandleConnectionException;
            }
            catch (Exception exception)
            {
                // A provider may register the listener before throwing. The agent still owns cleanup.
                RemoveListener();
                LogLifecycleFailure(exception, "Registering ActiveMQ connection exception listener failed");
                HandleConnectionException(exception);
                return;
            }

            try
            {
                await contextHandle.Completed.ConfigureAwait(false);
            }
            finally
            {
                // Retain the listener during a failed cleanup attempt so a later fault can retry it.
                RemoveListener();
            }
        }

        return contextHandle;
    }

    IActivePipeContextAgent<ConnectionContext> IPipeContextFactory<ConnectionContext>.CreateActiveContext(ISupervisor supervisor,
        IPipeContextHandle<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionAsync(context.Context, cancellationToken));
    }

    static async Task<ConnectionContext> CreateSharedConnectionAsync(Task<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully
            ? new SharedConnectionContext(context.Result, cancellationToken)
            : new SharedConnectionContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    ConnectionContext CreateConnection(ISupervisor supervisor)
    {
        var description = _hostConfiguration.Settings.ToDescription();

        if (supervisor.Stopping.IsCancellationRequested)
            throw new ActiveMqConnectionException($"The connection is stopping and cannot be used: {description}");

        IConnection? connection = null;
        try
        {
            TransportLogMessages.ConnectHost(description);

            connection = _hostConfiguration.Settings.CreateConnection();

            connection.Start();

            LogContext.Debug?.Log("Connected: {Host} (client-id: {ClientId}, version: {Version})", description,
                connection.ClientId, connection.MetaData.NMSVersion);

            return new ActiveMqConnectionContext(connection, _hostConfiguration, supervisor.Stopped);
        }
        catch (OperationCanceledException)
        {
            DisposeFailedConnection(connection);
            throw;
        }
        catch (NMSConnectionException ex)
        {
            DisposeFailedConnection(connection);
            LogConnectionFailure(ex);
            throw new ActiveMqConnectionException("Connection exception: " + description, ex);
        }
        catch (Exception ex)
        {
            DisposeFailedConnection(connection);
            LogConnectionFailure(ex);
            throw new ActiveMqConnectionException("Create Connection Faulted: " + description, ex);
        }
    }

    static void DisposeFailedConnection(IConnection? connection)
    {
        try
        {
            connection?.Dispose();
        }
        catch (Exception exception)
        {
            try
            {
                LogContext.Error?.Log(exception, "Disposing failed ActiveMQ connection failed");
            }
            catch
            {
                // Diagnostics must not replace the original connection failure or cancellation.
            }
        }
    }

    static void LogLifecycleFailure(Exception exception, string message)
    {
        try
        {
            LogContext.Error?.Log(exception, message);
        }
        catch
        {
            // A failed diagnostic sink must not escape an owned lifecycle operation.
        }
    }

    void LogConnectionFailure(Exception exception)
    {
        try
        {
            LogContext.Warning?.Log(exception, "Connection Failed: {InputAddress}", _hostConfiguration.HostAddress);
        }
        catch
        {
            // The original failure determines retry behavior even if its logger fails.
        }
    }
}
