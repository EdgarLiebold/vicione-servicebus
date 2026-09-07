using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Creates and shares supervised Event Hubs connection contexts.</summary>
public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly Action<EventHubProducerClientOptions>? _configureOptions;
    readonly IHostSettings _hostSettings;
    readonly IStorageSettings _storageSettings;

    /// <summary>Creates a factory from namespace, storage, and producer client settings.</summary>
    /// <param name="hostSettings">The Event Hubs namespace authentication settings.</param>
    /// <param name="storageSettings">The Blob Storage checkpoint settings carried by each context.</param>
    /// <param name="configureOptions">The optional producer client options callback.</param>
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
        IPipeContextHandle<ConnectionContext> context, CancellationToken cancellationToken)
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
