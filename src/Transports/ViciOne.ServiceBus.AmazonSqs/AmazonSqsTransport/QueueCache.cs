using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Resolves and caches Amazon SQS queue metadata with distinct durable and evictable ownership.</summary>
public sealed class QueueCache :
    IAsyncDisposable
{
    static readonly List<string> AllAttributes = [QueueAttributeName.All];

    readonly IAmazonSQS _client;
    readonly DurableResourceStore<string, QueueInfo> _durableQueues;
    readonly KeyedResourceCache<string, QueueInfo> _ephemeralQueues;

    /// <summary>Initializes an Amazon SQS queue cache.</summary>
    /// <param name="client">The Amazon SQS client used to resolve and create queues.</param>
    /// <param name="options">The capacity and lifetime settings for evictable entries.</param>
    /// <param name="lifetimeCancellationToken">The token that ends durable resource ownership.</param>
    public QueueCache(IAmazonSQS client, AmazonSqsClientContextCacheOptions options, CancellationToken lifetimeCancellationToken)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentNullException.ThrowIfNull(options);

        _durableQueues = new DurableResourceStore<string, QueueInfo>(lifetimeCancellationToken, StringComparer.Ordinal);
        _ephemeralQueues = new KeyedResourceCache<string, QueueInfo>(x => x.EntityName,
            options.CreateResourceCacheOptions(lifetimeCancellationToken), StringComparer.Ordinal);
    }

    /// <summary>Disposes evictable and durable queue metadata resources.</summary>
    /// <returns>A task that completes when both cache partitions have been disposed.</returns>
    public async ValueTask DisposeAsync()
    {
        await _ephemeralQueues.DisposeAsync().ConfigureAwait(false);
        await _durableQueues.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Gets or creates queue metadata using the topology entity's attributes, tags, and lifetime.</summary>
    /// <param name="queue">The queue topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel this caller's wait.</param>
    /// <returns>The resolved queue metadata.</returns>
    public async Task<QueueInfo> GetAsync(Queue queue, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queue);

        if (_durableQueues.TryGet(queue.EntityName, out var durable))
            return durable;

        if (queue is { Durable: true, AutoDelete: false })
        {
            // Ownership transition is explicit: a resource cannot be simultaneously evictable and durable.
            await _ephemeralQueues.RemoveAsync(queue.EntityName, cancellationToken).ConfigureAwait(false);

            return await _durableQueues.GetOrAddAsync(queue.EntityName,
                (_, ownerToken) => ResolveQueueAsync(queue, ownerToken), cancellationToken).ConfigureAwait(false);
        }

        return await _ephemeralQueues.GetOrAddAsync(queue.EntityName,
            (_, ownerToken) => ResolveQueueAsync(queue, ownerToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets metadata for an existing queue by logical name.</summary>
    /// <param name="entityName">The logical queue name.</param>
    /// <param name="cancellationToken">The token used to cancel this caller's wait.</param>
    /// <returns>The resolved queue metadata.</returns>
    public async Task<QueueInfo> GetByNameAsync(string entityName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        if (_durableQueues.TryGet(entityName, out var durable))
            return durable;

        return await _ephemeralQueues.GetOrAddAsync(entityName,
            (name, ownerToken) => GetExistingQueueAsync(name, ownerToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes and disposes queue metadata from either cache partition.</summary>
    /// <param name="entityName">The logical queue name.</param>
    /// <param name="cancellationToken">The token used to cancel removal from the evictable cache.</param>
    /// <returns><see langword="true"/> when an entry was removed; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RemoveByNameAsync(string entityName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        bool durableRemoved = await _durableQueues.RemoveAsync(entityName).ConfigureAwait(false);
        bool ephemeralRemoved = await _ephemeralQueues.RemoveAsync(entityName, cancellationToken: cancellationToken).ConfigureAwait(false);
        return durableRemoved || ephemeralRemoved;
    }

    async ValueTask<QueueInfo> ResolveQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        try
        {
            return await GetExistingQueueAsync(queue.EntityName, cancellationToken).ConfigureAwait(false);
        }
        catch (QueueDoesNotExistException)
        {
            return await CreateMissingQueueAsync(queue, cancellationToken).ConfigureAwait(false);
        }
    }

    async ValueTask<QueueInfo> CreateMissingQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        Dictionary<string, string> attributes = queue.QueueAttributes.ToDictionary(x => x.Key, x => x.Value.ToString()!);

        if (AmazonSqsEndpointAddress.IsFifo(queue.EntityName) && !attributes.ContainsKey(QueueAttributeName.FifoQueue))
        {
            LogContext.Warning?.Log("Using '.fifo' suffix without 'FifoQueue' attribute might cause unexpected behavior.");
            attributes[QueueAttributeName.FifoQueue] = "true";
        }

        var request = new CreateQueueRequest(queue.EntityName)
        {
            Attributes = attributes,
            Tags = queue.QueueTags.ToDictionary(x => x.Key, x => x.Value)
        };

        var createResponse = await _client.CreateQueueAsync(request, cancellationToken).ConfigureAwait(false);
        createResponse.EnsureSuccessfulResponse();

        var attributesResponse = await _client.GetQueueAttributesAsync(createResponse.QueueUrl, AllAttributes, cancellationToken).ConfigureAwait(false);
        attributesResponse.EnsureSuccessfulResponse();

        return new QueueInfo(queue.EntityName, createResponse.QueueUrl, attributesResponse.Attributes ?? new Dictionary<string, string>(),
            _client, cancellationToken, false);
    }

    async ValueTask<QueueInfo> GetExistingQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        var urlResponse = await _client.GetQueueUrlAsync(queueName, cancellationToken).ConfigureAwait(false);
        urlResponse.EnsureSuccessfulResponse();

        var attributesResponse = await _client.GetQueueAttributesAsync(urlResponse.QueueUrl, AllAttributes, cancellationToken).ConfigureAwait(false);
        attributesResponse.EnsureSuccessfulResponse();

        return new QueueInfo(queueName, urlResponse.QueueUrl, attributesResponse.Attributes ?? new Dictionary<string, string>(), _client,
            cancellationToken, true);
    }
}
