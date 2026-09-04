using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ScopeSessionContextFactory :
    IPipeContextFactory<SessionContext>
{
    readonly ISessionContextSupervisor _supervisor;

    public ScopeSessionContextFactory(ISessionContextSupervisor supervisor)
    {
        _supervisor = supervisor;
    }

    IPipeContextAgent<SessionContext> IPipeContextFactory<SessionContext>.CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<SessionContext> asyncContext = supervisor.AddAsyncContext<SessionContext>();

        Task<SessionContext> context = CreateSessionAsync(asyncContext, supervisor.Stopped);

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

        context.GetAwaiter().OnCompleted(() =>
        {
            if (!context.IsCompletedSuccessfully)
                return;

            var sessionContext = context.Result;
            sessionContext.ConnectionContext.Connection.ExceptionListener += HandleConnectionException;

            asyncContext.Completed.GetAwaiter().OnCompleted(() =>
                sessionContext.ConnectionContext.Connection.ExceptionListener -= HandleConnectionException);
        });

        return asyncContext;
    }

    IActivePipeContextAgent<SessionContext> IPipeContextFactory<SessionContext>.CreateActiveContext(ISupervisor supervisor,
        PipeContextHandle<SessionContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedSessionAsync(context.Context, cancellationToken));
    }

    static async Task<SessionContext> CreateSharedSessionAsync(Task<SessionContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedSessionContext(context.Result, cancellationToken)
            : new SharedSessionContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    Task<SessionContext> CreateSessionAsync(IAsyncPipeContextAgent<SessionContext> asyncContext, CancellationToken cancellationToken)
    {
        static Task<SessionContext> CreateSessionContextAsync(SessionContext context, CancellationToken createCancellationToken)
        {
            return Task.FromResult<SessionContext>(new SharedSessionContext(context, createCancellationToken));
        }

        return _supervisor.CreateAgentAsync(asyncContext, CreateSessionContextAsync, cancellationToken);
    }
}
