using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.SqlTransport;

public class SharedClientContextFactory :
    IPipeContextFactory<ClientContext>
{
    readonly IClientContextSupervisor _supervisor;

    public SharedClientContextFactory(IClientContextSupervisor supervisor)
    {
        _supervisor = supervisor;
    }

    IPipeContextAgent<ClientContext> IPipeContextFactory<ClientContext>.CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ClientContext> asyncContext = supervisor.AddAsyncContext<ClientContext>();

        CreateClientContext(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    IActivePipeContextAgent<ClientContext> IPipeContextFactory<ClientContext>.CreateActiveContext(ISupervisor supervisor,
        PipeContextHandle<ClientContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateScopeContextAsync(context.Context, cancellationToken));
    }

    static async Task<ClientContext> CreateScopeContextAsync(Task<ClientContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedClientContext(context.Result, cancellationToken)
            : new SharedClientContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    void CreateClientContext(IAsyncPipeContextAgent<ClientContext> asyncContext, CancellationToken cancellationToken)
    {
        static Task<ClientContext> CreateAsync(ClientContext context, CancellationToken createCancellationToken)
        {
            return Task.FromResult<ClientContext>(new SharedClientContext(context, createCancellationToken));
        }

        _supervisor.StartAgent(asyncContext, CreateAsync, cancellationToken);
    }
}
