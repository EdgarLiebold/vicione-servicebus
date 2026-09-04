using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public class ClientContextFactory :
    IPipeContextFactory<ClientContext>
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;

    public ClientContextFactory(IConnectionContextSupervisor connectionContextSupervisor)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
    }

    public IPipeContextAgent<ClientContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ClientContext> asyncContext = supervisor.AddAsyncContext<ClientContext>();

        CreateClientContext(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    public IActivePipeContextAgent<ClientContext> CreateActiveContext(ISupervisor supervisor,
        PipeContextHandle<ClientContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedClientContextAsync(context.Context, cancellationToken));
    }

    static async Task<ClientContext> CreateSharedClientContextAsync(Task<ClientContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new ScopeClientContext(context.Result, cancellationToken)
            : new ScopeClientContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    void CreateClientContext(IAsyncPipeContextAgent<ClientContext> asyncContext, CancellationToken cancellationToken)
    {
        static Task<ClientContext> CreateAsync(ConnectionContext connectionContext, CancellationToken createCancellationToken)
        {
            return Task.FromResult(connectionContext.CreateClientContext(createCancellationToken));
        }

        _connectionContextSupervisor.StartAgent(asyncContext, CreateAsync, cancellationToken);
    }
}
