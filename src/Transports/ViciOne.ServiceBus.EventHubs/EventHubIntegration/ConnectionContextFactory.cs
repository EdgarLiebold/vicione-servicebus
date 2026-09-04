using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a connection context factory implementation.
/// </summary>
public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly Action<EventHubProducerClientOptions>? _configureOptions;
    readonly IHostSettings _hostSettings;
    readonly IStorageSettings _storageSettings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostSettings">The host settings value.</param>
    /// <param name="storageSettings">The storage settings value.</param>
    /// <param name="configureOptions">The configure options value.</param>
    public ConnectionContextFactory(IHostSettings hostSettings, IStorageSettings storageSettings, Action<EventHubProducerClientOptions>? configureOptions)
    {
        _hostSettings = hostSettings;
        _storageSettings = storageSettings;
        _configureOptions = configureOptions;
    }

    IPipeContextAgent<ConnectionContext> IPipeContextFactory<ConnectionContext>.CreateContext(ISupervisor supervisor)
    {
        Task<ConnectionContext> context = Task.FromResult(CreateConnectionContext(supervisor));

        IPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddContext(context);

        return contextHandle;
    }

    IActivePipeContextAgent<ConnectionContext> IPipeContextFactory<ConnectionContext>.CreateActiveContext(ISupervisor supervisor,
        PipeContextHandle<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionContextAsync(context.Context, cancellationToken));
    }

    static async Task<ConnectionContext> CreateSharedConnectionContextAsync(Task<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedConnectionContext(context.Result, cancellationToken)
            : new SharedConnectionContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    ConnectionContext CreateConnectionContext(ISupervisor supervisor)
    {
        return new EventHubConnectionContext(_hostSettings, _storageSettings, _configureOptions, supervisor.Stopped);
    }
}
