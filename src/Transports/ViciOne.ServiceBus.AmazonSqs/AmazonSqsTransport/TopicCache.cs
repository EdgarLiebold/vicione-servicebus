using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Loads, creates, and caches Amazon SNS topic metadata with distinct durable and evictable ownership.</summary>
public sealed class TopicCache :
    IAsyncDisposable
{
    readonly CancellationToken _lifetimeCancellationToken;
    readonly IAmazonSimpleNotificationService _client;
    readonly object _loaderSync = new();
    readonly DurableResourceStore<string, TopicInfo> _durableTopics;
    readonly KeyedResourceCache<string, TopicInfo> _ephemeralTopics;
    Lazy<Task> _loadExistingTopics;
    volatile bool _topicsLoaded;

    /// <summary>Initializes an Amazon SNS topic cache.</summary>
    /// <param name="client">The Amazon SNS client used to list and create topics.</param>
    /// <param name="options">The capacity and lifetime settings for evictable entries.</param>
    /// <param name="lifetimeCancellationToken">The token that ends durable resource ownership and topic discovery.</param>
    public TopicCache(IAmazonSimpleNotificationService client, AmazonSqsClientContextCacheOptions options,
        CancellationToken lifetimeCancellationToken)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentNullException.ThrowIfNull(options);

        _lifetimeCancellationToken = lifetimeCancellationToken;
        _durableTopics = new DurableResourceStore<string, TopicInfo>(lifetimeCancellationToken, StringComparer.Ordinal);
        _ephemeralTopics = new KeyedResourceCache<string, TopicInfo>(x => x.EntityName,
            options.CreateResourceCacheOptions(lifetimeCancellationToken), StringComparer.Ordinal);
        _loadExistingTopics = CreateExistingTopicsLoader();
    }

    /// <summary>Disposes evictable and durable topic metadata resources.</summary>
    /// <returns>A task that completes when both cache partitions have been disposed.</returns>
    public async ValueTask DisposeAsync()
    {
        await _ephemeralTopics.DisposeAsync().ConfigureAwait(false);
        await _durableTopics.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Gets or creates topic metadata using the topology entity's attributes, tags, and lifetime.</summary>
    /// <param name="topic">The topic topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel topic discovery or this caller's wait.</param>
    /// <returns>The resolved topic metadata.</returns>
    public async Task<TopicInfo> GetAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(topic);
        await EnsureTopicsLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (_durableTopics.TryGet(topic.EntityName, out var durable))
            return durable;

        if (topic is { Durable: true, AutoDelete: false })
        {
            await _ephemeralTopics.RemoveAsync(topic.EntityName, cancellationToken).ConfigureAwait(false);

            return await _durableTopics.GetOrAddAsync(topic.EntityName,
                (_, ownerToken) => CreateMissingTopicAsync(topic, ownerToken), cancellationToken).ConfigureAwait(false);
        }

        return await _ephemeralTopics.GetOrAddAsync(topic.EntityName,
            (_, ownerToken) => CreateMissingTopicAsync(topic, ownerToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets metadata for an existing Amazon SNS topic by logical name.</summary>
    /// <param name="entityName">The logical topic name.</param>
    /// <param name="cancellationToken">The token used to cancel topic discovery or this caller's wait.</param>
    /// <returns>The resolved topic metadata.</returns>
    public async Task<TopicInfo> GetByNameAsync(string entityName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        await EnsureTopicsLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (_durableTopics.TryGet(entityName, out var durable))
            return durable;

        return await _ephemeralTopics.GetAsync(entityName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes and disposes topic metadata from either cache partition.</summary>
    /// <param name="entityName">The logical topic name.</param>
    /// <param name="cancellationToken">The token used to cancel removal from the evictable cache.</param>
    /// <returns><see langword="true"/> when an entry was removed; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RemoveByNameAsync(string entityName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        bool durableRemoved = await _durableTopics.RemoveAsync(entityName).ConfigureAwait(false);
        bool ephemeralRemoved = await _ephemeralTopics.RemoveAsync(entityName, cancellationToken: cancellationToken).ConfigureAwait(false);
        return durableRemoved || ephemeralRemoved;
    }

    async ValueTask<TopicInfo> CreateMissingTopicAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        var request = new CreateTopicRequest(topic.EntityName)
        {
            Attributes = topic.TopicAttributes.ToDictionary(x => x.Key, x => x.Value.ToString()),
            Tags = topic.TopicTags.Select(x => new Tag
            {
                Key = x.Key,
                Value = x.Value
            }).ToList()
        };

        var createResponse = await _client.CreateTopicAsync(request, cancellationToken).ConfigureAwait(false);
        createResponse.EnsureSuccessfulResponse();

        var attributesResponse = await _client.GetTopicAttributesAsync(createResponse.TopicArn, cancellationToken).ConfigureAwait(false);
        attributesResponse.EnsureSuccessfulResponse();

        return new TopicInfo(topic.EntityName, createResponse.TopicArn, _client, cancellationToken, false);
    }

    Lazy<Task> CreateExistingTopicsLoader()
    {
        return new Lazy<Task>(() => LoadExistingTopicsAsync(_lifetimeCancellationToken), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    async Task EnsureTopicsLoadedAsync(CancellationToken cancellationToken)
    {
        if (_topicsLoaded)
            return;

        while (true)
        {
            Lazy<Task> loader = _loadExistingTopics;
            try
            {
                await loader.Value.OrCanceledAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch when (loader.Value.IsFaulted || loader.Value.IsCanceled)
            {
                lock (_loaderSync)
                {
                    if (ReferenceEquals(_loadExistingTopics, loader))
                        _loadExistingTopics = CreateExistingTopicsLoader();
                }

                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

    async Task LoadExistingTopicsAsync(CancellationToken cancellationToken)
    {
        string? cursor = null;
        do
        {
            var response = await _client.ListTopicsAsync(new ListTopicsRequest { NextToken = cursor }, cancellationToken).ConfigureAwait(false);

            if (response.Topics is not null)
            {
                foreach (var topic in response.Topics)
                {
                    int index = topic.TopicArn.LastIndexOf(':');
                    if (index < 0 || index == topic.TopicArn.Length - 1)
                        continue;

                    string topicName = topic.TopicArn[(index + 1)..];
                    await _durableTopics.GetOrAddAsync(topicName,
                        (_, ownerToken) => ValueTask.FromResult(new TopicInfo(topicName, topic.TopicArn, _client, ownerToken, true)),
                        cancellationToken).ConfigureAwait(false);
                }
            }

            cursor = response.NextToken;
        }
        while (!string.IsNullOrEmpty(cursor));

        _topicsLoaded = true;
    }
}
