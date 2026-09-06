using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Owns resolved Amazon SNS topic metadata and its lazy publish batcher.</summary>
public class TopicInfo :
    IAsyncDisposable,
    ViciOne.ServiceBus.Caching.IResourceUsageSource
{
    readonly Lazy<IBatcher<PublishBatchRequestEntry>> _batchPublisher;
    bool _disposed;

    /// <summary>Initializes resolved topic metadata and a lazy publish batcher.</summary>
    /// <param name="entityName">The logical topic name.</param>
    /// <param name="arn">The Amazon SNS topic ARN.</param>
    /// <param name="client">The Amazon SNS client used for publishing.</param>
    /// <param name="cancellationToken">The token used to cancel provider requests issued by the lazy batcher.</param>
    /// <param name="existing">Whether the topic existed before it was resolved.</param>
    public TopicInfo(string entityName, string arn, IAmazonSimpleNotificationService client, CancellationToken cancellationToken, bool existing)
    {
        EntityName = entityName;
        Arn = arn;
        Existing = existing;

        _batchPublisher = new Lazy<IBatcher<PublishBatchRequestEntry>>(() => new PublishBatcher(client, arn, cancellationToken));
    }

    /// <summary>Gets the logical topic name.</summary>
    public string EntityName { get; }
    /// <summary>Gets the Amazon SNS topic ARN.</summary>
    public string Arn { get; }
    /// <summary>Gets whether the topic existed before it was resolved.</summary>
    public bool Existing { get; }

    /// <summary>Occurs when an operation uses this topic metadata resource.</summary>
    public event Action? Used;

    /// <summary>Disposes the publish batcher when it has been initialized.</summary>
    /// <returns>A task that completes when the initialized batcher has drained and stopped.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_batchPublisher.IsValueCreated)
            await _batchPublisher.Value.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Queues an Amazon SNS publish entry and waits for its batch result.</summary>
    /// <param name="entry">The publish batch entry.</param>
    /// <param name="cancellationToken">The token used to cancel admission to the batch queue.</param>
    /// <returns>A task that completes when Amazon SNS reports the entry result.</returns>
    public Task PublishAsync(PublishBatchRequestEntry entry, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        return _batchPublisher.Value.ExecuteAsync(entry, cancellationToken);
    }
}
