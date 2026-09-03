namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Agents;
    using Apache.NMS;
    using Configuration;
    using Internals;
    using Transports;


    public class ConnectionContextFactory :
        IPipeContextFactory<ConnectionContext>
    {
        readonly IActiveMqHostConfiguration _hostConfiguration;

        public ConnectionContextFactory(IActiveMqHostConfiguration hostConfiguration)
        {
            _hostConfiguration = hostConfiguration;
        }

        IPipeContextAgent<ConnectionContext> IPipeContextFactory<ConnectionContext>.CreateContext(ISupervisor supervisor)
        {
            Task<ConnectionContext> context = Task.Run(() => CreateConnection(supervisor), supervisor.Stopped);

            IPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddContext(context);

            var faultStopLock = new object();
            Task faultStopTask = null;

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
                _ = StopAfterConnectionException(exception, stopCompletion);
            }

            async Task StopAfterConnectionException(Exception exception, TaskCompletionSource stopCompletion)
            {
                try
                {
                    await contextHandle.Stop($"Connection Exception: {exception}").ConfigureAwait(false);
                }
                catch (Exception stopException)
                {
                    LogContext.Error?.Log(stopException, "Stopping faulted ActiveMQ connection context failed");
                }
                finally
                {
                    stopCompletion.TrySetResult();
                }
            }

            context.GetAwaiter().OnCompleted(() =>
            {
                if (!context.IsCompletedSuccessfully)
                    return;

                var connectionContext = context.Result;
                connectionContext.Connection.ExceptionListener += HandleConnectionException;

                contextHandle.Completed.GetAwaiter().OnCompleted(() =>
                    connectionContext.Connection.ExceptionListener -= HandleConnectionException);
            });

            return contextHandle;
        }

        IActivePipeContextAgent<ConnectionContext> IPipeContextFactory<ConnectionContext>.CreateActiveContext(ISupervisor supervisor,
            PipeContextHandle<ConnectionContext> context, CancellationToken cancellationToken)
        {
            return supervisor.AddActiveContext(context, CreateSharedConnection(context.Context, cancellationToken));
        }

        static async Task<ConnectionContext> CreateSharedConnection(Task<ConnectionContext> context, CancellationToken cancellationToken)
        {
            return context.IsCompletedSuccessfully()
                ? new SharedConnectionContext(context.Result, cancellationToken)
                : new SharedConnectionContext(await context.OrCanceled(cancellationToken).ConfigureAwait(false), cancellationToken);
        }

        async Task<ConnectionContext> CreateConnection(ISupervisor supervisor)
        {
            var description = _hostConfiguration.Settings.ToDescription();

            if (supervisor.Stopping.IsCancellationRequested)
                throw new ActiveMqConnectionException($"The connection is stopping and cannot be used: {description}");

            IConnection connection = null;
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
                connection?.Dispose();
                throw;
            }
            catch (NMSConnectionException ex)
            {
                connection?.Dispose();
                LogContext.Warning?.Log(ex, "Connection Failed: {InputAddress}", _hostConfiguration.HostAddress);
                throw new ActiveMqConnectionException("Connection exception: " + description, ex);
            }
            catch (Exception ex)
            {
                connection?.Dispose();
                LogContext.Warning?.Log(ex, "Connection Failed: {InputAddress}", _hostConfiguration.HostAddress);
                throw new ActiveMqConnectionException("Create Connection Faulted: " + description, ex);
            }
        }
    }
}
