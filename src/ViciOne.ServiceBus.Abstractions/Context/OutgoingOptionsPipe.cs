using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

internal sealed class SendOptionsPipe<TMessage>(SendOptions options) :
    IPipe<SendContext<TMessage>>
    where TMessage : class
{
    private readonly OutgoingOptionsSnapshot _options = OutgoingOptionsSnapshot.Create(options);

    public Task SendAsync(SendContext<TMessage> context)
    {
        OutgoingOptionsPipe.Apply(context, _options);
        return Task.CompletedTask;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("sendOptions");
    }
}

internal sealed class PublishOptionsPipe<TMessage>(PublishOptions options) :
    IPipe<PublishContext<TMessage>>
    where TMessage : class
{
    private readonly OutgoingOptionsSnapshot _options = OutgoingOptionsSnapshot.Create(options);

    public Task SendAsync(PublishContext<TMessage> context)
    {
        OutgoingOptionsPipe.Apply(context, _options);
        return Task.CompletedTask;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("publishOptions");
    }
}

internal sealed class ScheduleOptionsPipe<TMessage>(ScheduleOptions options) :
    IPipe<SendContext<TMessage>>
    where TMessage : class
{
    private readonly OutgoingOptionsSnapshot _options = OutgoingOptionsSnapshot.Create(options);

    public Task SendAsync(SendContext<TMessage> context)
    {
        OutgoingOptionsPipe.Apply(context, _options);
        return Task.CompletedTask;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scheduleOptions");
    }
}

static class OutgoingOptionsPipe
{
    public static void Apply(SendContext context, OutgoingOptionsSnapshot options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        PartitionKeySendContext? partitionContext = null;
        if (options.PartitionKey is not null && !context.TryGetPayload(out partitionContext))
            throw new NotSupportedException("The selected transport does not support partition keys.");

        foreach ((string key, object? value) in options.Headers)
            context.Headers.Set(key, value);

        if (options.TimeToLive.HasValue)
            context.TimeToLive = options.TimeToLive;
        if (options.CorrelationId.HasValue)
            context.CorrelationId = options.CorrelationId;
        if (options.ConversationId.HasValue)
            context.ConversationId = options.ConversationId;
        if (options.MessageId.HasValue)
            context.MessageId = options.MessageId;
        if (options.RequestId.HasValue)
            context.RequestId = options.RequestId;
        if (partitionContext is not null)
            partitionContext.PartitionKey = options.PartitionKey;
    }
}

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
