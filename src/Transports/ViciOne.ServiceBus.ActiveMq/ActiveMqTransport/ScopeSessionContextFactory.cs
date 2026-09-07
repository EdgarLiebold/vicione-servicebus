using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Creates operation-scoped proxies over a shared ActiveMQ session context.</summary>
public class ScopeSessionContextFactory :
    IPipeContextFactory<SessionContext>
{
    readonly ISessionContextSupervisor _supervisor;

    /// <summary>Creates a scope factory backed by a parent session supervisor.</summary>
    /// <param name="supervisor">The parent session supervisor.</param>
    public ScopeSessionContextFactory(ISessionContextSupervisor supervisor)
    {
        _supervisor = supervisor;
    }

    IPipeContextAgent<SessionContext> IPipeContextFactory<SessionContext>.CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<SessionContext> asyncContext = supervisor.AddAsyncContext<SessionContext>();

        var faultStopLock = new object();
        Task? faultStopTask = null;

        void HandleConnectionException(Exception exception)
        {
            // A send transport caches this shared session independently from the receive
            // endpoint. Release that cache at the same causal boundary as the underlying
            // connection, otherwise a retained ISendEndpoint can reuse a disposed session
            // executor after the receive endpoint has already recovered.
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
                await asyncContext.StopAsync($"Connection Exception: {exception}").ConfigureAwait(false);
            }
            catch (Exception stopException)
            {
                LogContext.Error?.Log(stopException, "Stopping faulted ActiveMQ session context failed");
            }
        }

        Task<SessionContext> CreateSharedSessionContextAsync(SessionContext sessionContext,
            CancellationToken createCancellationToken)
        {
            var sharedSessionContext = new SharedSessionContext(sessionContext, createCancellationToken);

            sessionContext.ConnectionContext.Connection.ExceptionListener += HandleConnectionException;

            asyncContext.Completed.GetAwaiter().OnCompleted(() =>
                sessionContext.ConnectionContext.Connection.ExceptionListener -= HandleConnectionException);

            return Task.FromResult<SessionContext>(sharedSessionContext);
        }

        // Installing the fault listener inside the agent factory makes it visible before the shared
        // context is published, so every cached session is fault-observable from its first use.
        _supervisor.StartAgent(asyncContext, CreateSharedSessionContextAsync, supervisor.Stopped);

        return asyncContext;
    }

    IActivePipeContextAgent<SessionContext> IPipeContextFactory<SessionContext>.CreateActiveContext(ISupervisor supervisor,
        IPipeContextHandle<SessionContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedSessionAsync(context.Context, cancellationToken));
    }

    static async Task<SessionContext> CreateSharedSessionAsync(Task<SessionContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedSessionContext(context.Result, cancellationToken)
            : new SharedSessionContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

}
