using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class SessionContextFactory :
    IPipeContextFactory<SessionContext>
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;

    public SessionContextFactory(IConnectionContextSupervisor connectionContextSupervisor)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
    }

    public IPipeContextAgent<SessionContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<SessionContext> asyncContext = supervisor.AddAsyncContext<SessionContext>();

        CreateSession(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    public IActivePipeContextAgent<SessionContext> CreateActiveContext(ISupervisor supervisor,
        PipeContextHandle<SessionContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedSessionAsync(context.Context, cancellationToken));
    }

    static async Task<SessionContext> CreateSharedSessionAsync(Task<SessionContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new ScopeSessionContext(context.Result, cancellationToken)
            : new ScopeSessionContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    void CreateSession(IAsyncPipeContextAgent<SessionContext> asyncContext, CancellationToken cancellationToken)
    {
        async Task<SessionContext> CreateSessionContextAsync(ConnectionContext connectionContext, CancellationToken createCancellationToken)
        {
            var session = await connectionContext.CreateSessionAsync(createCancellationToken).ConfigureAwait(false);

            var faultStopLock = new object();
            Task? faultStopTask = null;

            void HandleConnectionException(Exception exception)
            {
                lock (faultStopLock)
                {
                    if (faultStopTask == null || faultStopTask.IsCompleted)
                        faultStopTask = StopAfterConnectionExceptionAsync(exception);
                }
            }

            async Task StopAfterConnectionExceptionAsync(Exception exception)
            {
                await Task.Yield();

                try
                {
                    await asyncContext.StopAsync($"Connection Exception: {exception}", cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception stopException)
                {
                    LogContext.Error?.Log(stopException, "Stopping faulted ActiveMQ session context failed");
                }
            }

            connectionContext.Connection.ExceptionListener += HandleConnectionException;

            asyncContext.Completed.GetAwaiter().OnCompleted(() =>
                connectionContext.Connection.ExceptionListener -= HandleConnectionException);

            return new ActiveMqSessionContext(connectionContext, session, createCancellationToken);
        }

        _connectionContextSupervisor.StartAgent(asyncContext, CreateSessionContextAsync, cancellationToken);
    }
}
