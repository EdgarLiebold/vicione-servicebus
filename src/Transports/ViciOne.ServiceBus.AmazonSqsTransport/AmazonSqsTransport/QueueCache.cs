namespace ViciOne.ServiceBus.AmazonSqsTransport;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Caching;
using Topology;


public sealed class QueueCache :
    IAsyncDisposable
{
    static readonly List<string> AllAttributes = [QueueAttributeName.All];

    readonly IAmazonSQS _client;
    readonly DurableResourceStore<string, QueueInfo> _durableQueues;
    readonly KeyedResourceCache<string, QueueInfo> _ephemeralQueues;

    public QueueCache(IAmazonSQS client, AmazonSqsClientContextCacheOptions options, CancellationToken lifetimeCancellationToken)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentNullException.ThrowIfNull(options);

        _durableQueues = new DurableResourceStore<string, QueueInfo>(lifetimeCancellationToken, StringComparer.Ordinal);
        _ephemeralQueues = new KeyedResourceCache<string, QueueInfo>(x => x.EntityName,
            options.CreateResourceCacheOptions(lifetimeCancellationToken), StringComparer.Ordinal);
    }

    public async ValueTask DisposeAsync()
    {
        await _ephemeralQueues.DisposeAsync().ConfigureAwait(false);
        await _durableQueues.DisposeAsync().ConfigureAwait(false);
    }

    public async Task<QueueInfo> Get(Queue queue, CancellationToken cancellationToken)
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

    public async Task<QueueInfo> GetByName(string entityName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        if (_durableQueues.TryGet(entityName, out var durable))
            return durable;

        return await _ephemeralQueues.GetOrAddAsync(entityName,
            (name, ownerToken) => GetExistingQueueAsync(name, ownerToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveByName(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        bool durableRemoved = await _durableQueues.RemoveAsync(entityName).ConfigureAwait(false);
        bool ephemeralRemoved = await _ephemeralQueues.RemoveAsync(entityName).ConfigureAwait(false);
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
