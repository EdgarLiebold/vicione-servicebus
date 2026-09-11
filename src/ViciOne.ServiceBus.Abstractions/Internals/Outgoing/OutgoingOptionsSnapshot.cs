using System.Collections.Frozen;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Internals.Outgoing;

/// <summary>Snapshots application-owned outgoing options before asynchronous transport work begins.</summary>
internal sealed class OutgoingOptionsSnapshot
{
    private OutgoingOptionsSnapshot(
        IReadOnlyDictionary<string, object?> headers,
        TimeSpan? timeToLive,
        Guid? correlationId,
        Guid? conversationId,
        Guid? messageId,
        Guid? requestId,
        string? partitionKey,
        string parameterName)
    {
        if (headers is null)
            throw new ArgumentException("The options header collection cannot be null.", parameterName);
        if (timeToLive is { } lifetime && lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(parameterName, lifetime, "The message time to live must be greater than zero.");

        Headers = headers.Count == 0
            ? FrozenDictionary<string, object?>.Empty
            : headers.ToFrozenDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
        TimeToLive = timeToLive;
        CorrelationId = correlationId;
        ConversationId = conversationId;
        MessageId = messageId;
        RequestId = requestId;
        PartitionKey = partitionKey;
    }

    public IReadOnlyDictionary<string, object?> Headers { get; }

    public TimeSpan? TimeToLive { get; }

    public Guid? CorrelationId { get; }

    public Guid? ConversationId { get; }

    public Guid? MessageId { get; }

    public Guid? RequestId { get; }

    public string? PartitionKey { get; }

    public static OutgoingOptionsSnapshot Create(SendOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Create(options.Headers, options.TimeToLive, options.CorrelationId, options.ConversationId,
            options.MessageId, options.RequestId, options.PartitionKey, nameof(options));
    }

    public static OutgoingOptionsSnapshot Create(PublishOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Create(options.Headers, options.TimeToLive, options.CorrelationId, options.ConversationId,
            options.MessageId, options.RequestId, options.PartitionKey, nameof(options));
    }

    public static OutgoingOptionsSnapshot Create(ScheduleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Create(options.Headers, options.TimeToLive, options.CorrelationId, options.ConversationId,
            options.MessageId, options.RequestId, options.PartitionKey, nameof(options));
    }

    public static OutgoingOptionsSnapshot Create(RequestOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Create(options.Headers, options.TimeToLive, options.CorrelationId, options.ConversationId,
            options.MessageId, options.RequestId, options.PartitionKey, nameof(options));
    }

    private static OutgoingOptionsSnapshot Create(
        IReadOnlyDictionary<string, object?> headers,
        TimeSpan? timeToLive,
        Guid? correlationId,
        Guid? conversationId,
        Guid? messageId,
        Guid? requestId,
        string? partitionKey,
        string parameterName) => new(
        headers,
        timeToLive,
        correlationId,
        conversationId,
        messageId,
        requestId,
        partitionKey,
        parameterName);
}
