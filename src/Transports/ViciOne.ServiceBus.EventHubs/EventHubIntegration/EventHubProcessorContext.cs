using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Owns an Event Hubs processor client and composes internal and application partition callbacks.</summary>
public class EventHubProcessorContext :
    BasePipeContext,
    ProcessorContext
{
    readonly EventProcessorClient _client;
    readonly IHostConfiguration _hostConfiguration;
    readonly Func<PartitionClosingEventArgs, Task>? _partitionClosingHandler;
    readonly Func<PartitionInitializingEventArgs, Task>? _partitionInitializingHandler;
    Action? _releaseClient;

    /// <summary>Creates a processor context for one receive endpoint.</summary>
    /// <param name="hostConfiguration">The bus host configuration used for receive logging.</param>
    /// <param name="client">The Azure SDK event processor client.</param>
    /// <param name="partitionInitializingHandler">The optional application partition-initializing handler.</param>
    /// <param name="partitionClosingHandler">The optional application partition-closing handler.</param>
    /// <param name="cancellationToken">Stops operations using this processor context.</param>
    public EventHubProcessorContext(IHostConfiguration hostConfiguration,
        EventProcessorClient client, Func<PartitionInitializingEventArgs, Task>? partitionInitializingHandler,
        Func<PartitionClosingEventArgs, Task>? partitionClosingHandler, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _hostConfiguration = hostConfiguration;

        _client = client;

        _partitionInitializingHandler = partitionInitializingHandler;
        _partitionClosingHandler = partitionClosingHandler;
    }

    /// <summary>Gets the initialized receive logging context.</summary>
    public ILogContext LogContext => _hostConfiguration.ReceiveLogContext
        ?? throw new InvalidOperationException("The receive log context has not been initialized.");

    /// <summary>Subscribes composed partition callbacks and leases the owned processor client.</summary>
    /// <param name="context">The internal callback target that maintains checkpoint state.</param>
    /// <returns>The owned processor client.</returns>
    public EventProcessorClient GetClient(ProcessorClientBuilderContext context)
    {
        if (_releaseClient != null)
            throw new InvalidOperationException("The client has already been configured and will throw an exception if it is used again");

        Func<PartitionInitializingEventArgs, Task> OnPartitionInitializingAsync()
        {
            return async args =>
            {
                await context.OnPartitionInitializingAsync(args).ConfigureAwait(false);
                if (_partitionInitializingHandler != null)
                    await _partitionInitializingHandler(args).ConfigureAwait(false);
            };
        }

        _client.PartitionInitializingAsync += OnPartitionInitializingAsync();

        Func<PartitionClosingEventArgs, Task> OnPartitionClosingAsync()
        {
            return async args =>
            {
                if (_partitionClosingHandler != null)
                    await _partitionClosingHandler(args).ConfigureAwait(false);

                await context.OnPartitionClosingAsync(args).ConfigureAwait(false);
            };
        }

        _client.PartitionClosingAsync += OnPartitionClosingAsync();

        _releaseClient = () =>
        {
            _client.PartitionClosingAsync -= OnPartitionClosingAsync();
            _client.PartitionInitializingAsync -= OnPartitionInitializingAsync();

            _releaseClient = null;
        };

        return _client;
    }

    /// <summary>Removes the partition callbacks installed by the current client lease.</summary>
    /// <param name="processorLockContext">The callback target whose lease is being released.</param>
    public void ReleaseClient(ProcessorClientBuilderContext processorLockContext)
    {
        _releaseClient?.Invoke();
    }
}
