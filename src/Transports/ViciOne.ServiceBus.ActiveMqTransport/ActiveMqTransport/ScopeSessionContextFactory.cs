namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Agents;
    using Internals;


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

            Task<SessionContext> context = CreateSession(asyncContext, supervisor.Stopped);

            void HandleConnectionException(Exception exception)
            {
                // A send transport caches this shared session independently from the receive
                // endpoint. Retire that cache at the same causal boundary as the underlying
                // connection, otherwise a retained ISendEndpoint can reuse a disposed session
                // executor after the receive endpoint has already recovered.
                asyncContext.Stop($"Connection Exception: {exception}");
            }

            context.ContinueWith(task =>
            {
                task.Result.ConnectionContext.Connection.ExceptionListener += HandleConnectionException;

                asyncContext.Completed.ContinueWith(
                    _ => task.Result.ConnectionContext.Connection.ExceptionListener -= HandleConnectionException,
                    TaskContinuationOptions.ExecuteSynchronously);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            return asyncContext;
        }

        IActivePipeContextAgent<SessionContext> IPipeContextFactory<SessionContext>.CreateActiveContext(ISupervisor supervisor,
            PipeContextHandle<SessionContext> context, CancellationToken cancellationToken)
        {
            return supervisor.AddActiveContext(context, CreateSharedSession(context.Context, cancellationToken));
        }

        static async Task<SessionContext> CreateSharedSession(Task<SessionContext> context, CancellationToken cancellationToken)
        {
            return context.IsCompletedSuccessfully()
                ? new SharedSessionContext(context.Result, cancellationToken)
                : new SharedSessionContext(await context.OrCanceled(cancellationToken).ConfigureAwait(false), cancellationToken);
        }

        Task<SessionContext> CreateSession(IAsyncPipeContextAgent<SessionContext> asyncContext, CancellationToken cancellationToken)
        {
            static Task<SessionContext> CreateSessionContext(SessionContext context, CancellationToken createCancellationToken)
            {
                return Task.FromResult<SessionContext>(new SharedSessionContext(context, createCancellationToken));
            }

            return _supervisor.CreateAgent(asyncContext, CreateSessionContext, cancellationToken);
        }
    }
}
