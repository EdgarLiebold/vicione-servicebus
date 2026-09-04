using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub processor context implementation.
/// </summary>
public class EventHubProcessorContext :
    BasePipeContext,
    ProcessorContext
{
    readonly EventProcessorClient _client;
    readonly IHostConfiguration _hostConfiguration;
    readonly Func<PartitionClosingEventArgs, Task>? _partitionClosingHandler;
    readonly Func<PartitionInitializingEventArgs, Task>? _partitionInitializingHandler;
    Action? _releaseClient;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="client">The client value.</param>
    /// <param name="partitionInitializingHandler">The partition initializing handler value.</param>
    /// <param name="partitionClosingHandler">The partition closing handler value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
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

    /// <summary>
    /// Gets the log context value.
    /// </summary>
    public ILogContext LogContext => _hostConfiguration.ReceiveLogContext
        ?? throw new InvalidOperationException("The receive log context has not been initialized.");

    /// <summary>
    /// Gets client.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the release client operation.
    /// </summary>
    /// <param name="processorLockContext">The processor lock context value.</param>
    public void ReleaseClient(ProcessorClientBuilderContext processorLockContext)
    {
        _releaseClient?.Invoke();
    }
}
