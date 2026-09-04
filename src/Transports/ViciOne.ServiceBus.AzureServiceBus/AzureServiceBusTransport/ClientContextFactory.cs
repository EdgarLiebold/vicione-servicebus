using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a client context factory implementation.
/// </summary>
public abstract class ClientContextFactory :
    IPipeContextFactory<ClientContext>
{
    readonly ClientSettings _settings;
    readonly IConnectionContextSupervisor _supervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="settings">The settings value.</param>
    protected ClientContextFactory(IConnectionContextSupervisor supervisor, ClientSettings settings)
    {
        _supervisor = supervisor;
        _settings = settings;
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
    public IActivePipeContextAgent<ClientContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<ClientContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedContextAsync(context.Context, cancellationToken));
    }

    /// <summary>
    /// Creates client context.
    /// </summary>
    /// <param name="connectionContext">The connection context value.</param>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="agent">The agent value.</param>
    /// <returns>The result of the operation.</returns>
    protected abstract ClientContext CreateClientContext(ConnectionContext connectionContext, Uri inputAddress, IAgent agent);

    void CreateClientContext(IAsyncPipeContextAgent<ClientContext> asyncContext, CancellationToken cancellationToken)
    {
        Task<ClientContext> CreateAsync(ConnectionContext connectionContext, CancellationToken createCancellationToken)
        {
            var inputAddress = _settings.GetInputAddress(connectionContext.Endpoint, _settings.Path);

            return Task.FromResult(CreateClientContext(connectionContext, inputAddress, asyncContext));
        }

        _supervisor.StartAgent(asyncContext, CreateAsync, cancellationToken);
    }

    static async Task<ClientContext> CreateSharedContextAsync(Task<ClientContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedClientContext(context.Result, cancellationToken)
            : new SharedClientContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }
}
