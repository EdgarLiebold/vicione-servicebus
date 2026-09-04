using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a topic info implementation.
/// </summary>
public class TopicInfo :
    IAsyncDisposable,
    ViciOne.ServiceBus.Caching.IResourceUsageSource
{
    readonly Lazy<IBatcher<PublishBatchRequestEntry>> _batchPublisher;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="entityName">The entity name value.</param>
    /// <param name="arn">The arn value.</param>
    /// <param name="client">The client value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="existing">The existing value.</param>
    public TopicInfo(string entityName, string arn, IAmazonSimpleNotificationService client, CancellationToken cancellationToken, bool existing)
    {
        EntityName = entityName;
        Arn = arn;
        Existing = existing;

        _batchPublisher = new Lazy<IBatcher<PublishBatchRequestEntry>>(() => new PublishBatcher(client, arn, cancellationToken));
    }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public string EntityName { get; }
    /// <summary>
    /// Gets the arn value.
    /// </summary>
    public string Arn { get; }
    /// <summary>
    /// Gets the existing value.
    /// </summary>
    public bool Existing { get; }

    /// <summary>
    /// Occurs when used.
    /// </summary>
    public event Action? Used;

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_batchPublisher.IsValueCreated)
            await _batchPublisher.Value.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync(PublishBatchRequestEntry entry, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        return _batchPublisher.Value.ExecuteAsync(entry, cancellationToken);
    }
}
