using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Creates shared and operation-scoped Amazon client contexts.</summary>
public class ClientContextFactory :
    IPipeContextFactory<ClientContext>
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;

    /// <summary>Initializes a client-context factory.</summary>
    /// <param name="connectionContextSupervisor">The supervisor that supplies the active connection context.</param>
    public ClientContextFactory(IConnectionContextSupervisor connectionContextSupervisor)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
    }

    /// <summary>Creates an asynchronously established client-context agent.</summary>
    /// <param name="supervisor">The supervisor that owns the agent.</param>
    /// <returns>The new client-context agent.</returns>
    public IPipeContextAgent<ClientContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ClientContext> asyncContext = supervisor.AddAsyncContext<ClientContext>();

        CreateClientContext(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    /// <summary>Creates an operation-scoped client context from a shared context handle.</summary>
    /// <param name="supervisor">The supervisor that owns the active context.</param>
    /// <param name="context">The shared client-context handle.</param>
    /// <param name="cancellationToken">The operation cancellation token assigned to the scoped context.</param>
    /// <returns>The active scoped-context agent.</returns>
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
