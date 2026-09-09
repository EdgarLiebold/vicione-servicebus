using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Creates and monitors Apache NMS session contexts on supervised connections.</summary>
public class SessionContextFactory :
    IPipeContextFactory<SessionContext>
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;

    /// <summary>Creates a session-context factory for a connection supervisor.</summary>
    /// <param name="connectionContextSupervisor">The parent connection supervisor.</param>
    public SessionContextFactory(IConnectionContextSupervisor connectionContextSupervisor)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
    }

    /// <summary>Creates a supervised session-context agent.</summary>
    /// <param name="supervisor">The supervisor that owns the agent.</param>
    /// <returns>The asynchronous session-context agent.</returns>
    public IPipeContextAgent<SessionContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<SessionContext> asyncContext = supervisor.AddAsyncContext<SessionContext>();

        CreateSession(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    /// <summary>Creates an operation-scoped proxy over a supervised session context.</summary>
    /// <param name="supervisor">The supervisor that owns the active context.</param>
    /// <param name="context">The underlying session-context handle.</param>
    /// <param name="cancellationToken">The token associated with the operation scope.</param>
    /// <returns>The active session-context agent.</returns>
    public IActivePipeContextAgent<SessionContext> CreateActiveContext(ISupervisor supervisor,
        IPipeContextHandle<SessionContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedSessionAsync(context.Context, cancellationToken));
    }

    static async Task<SessionContext> CreateSharedSessionAsync(Task<SessionContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully
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
