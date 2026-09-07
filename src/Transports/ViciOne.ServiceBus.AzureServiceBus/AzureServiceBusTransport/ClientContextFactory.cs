using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates supervised Azure Service Bus client contexts from connection contexts.</summary>
public abstract class ClientContextFactory :
    IPipeContextFactory<ClientContext>
{
    readonly ClientSettings _settings;
    readonly IConnectionContextSupervisor _supervisor;

    /// <summary>Initializes the factory with the connection supervisor and entity settings used for every client context.</summary>
    /// <param name="supervisor">The connection-context supervisor that supplies active connections.</param>
    /// <param name="settings">The settings that identify and configure the target entity.</param>
    protected ClientContextFactory(IConnectionContextSupervisor supervisor, ClientSettings settings)
    {
        _supervisor = supervisor;
        _settings = settings;
    }

    /// <summary>Creates and starts a supervised client-context agent.</summary>
    /// <param name="supervisor">The owning supervisor.</param>
    /// <returns>The agent that exposes the client context when its connection is ready.</returns>
    public IPipeContextAgent<ClientContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ClientContext> asyncContext = supervisor.AddAsyncContext<ClientContext>();

        CreateClientContext(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    /// <summary>Creates an active agent whose shared context is canceled with the caller's lease.</summary>
    /// <param name="supervisor">The owning supervisor.</param>
    /// <param name="context">The client-context handle to share.</param>
    /// <param name="cancellationToken">The token that ends the active lease.</param>
    /// <returns>An active agent over the shared client context.</returns>
    public IActivePipeContextAgent<ClientContext> CreateActiveContext(ISupervisor supervisor, IPipeContextHandle<ClientContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedContextAsync(context.Context, cancellationToken));
    }

    /// <summary>Creates the provider-specific client context for an established connection and entity address.</summary>
    /// <param name="connectionContext">The established namespace connection.</param>
    /// <param name="inputAddress">The resolved queue or subscription address.</param>
    /// <param name="agent">The agent notified when the context faults.</param>
    /// <returns>The provider-specific client context.</returns>
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
