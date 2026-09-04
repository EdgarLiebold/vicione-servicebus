using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a client context factory implementation.
/// </summary>
public class ClientContextFactory :
    IPipeContextFactory<ClientContext>
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContextSupervisor">The connection context supervisor value.</param>
    public ClientContextFactory(IConnectionContextSupervisor connectionContextSupervisor)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
    }

    /// <summary>
    /// Creates context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipeContextAgent<ClientContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ClientContext> asyncContext = supervisor.AddAsyncContext<ClientContext>();

        CreateClientContext(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    /// <summary>
    /// Creates active context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
