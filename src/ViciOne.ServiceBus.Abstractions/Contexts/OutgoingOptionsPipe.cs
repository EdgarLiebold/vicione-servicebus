using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

internal sealed class SendOptionsPipe<T>(SendOptions options) :
    IPipe<SendContext<T>>
    where T : class
{
    public Task SendAsync(SendContext<T> context)
    {
        OutgoingOptionsPipe.Apply(
            context,
            options.Headers,
            options.TimeToLive,
            options.CorrelationId,
            options.ConversationId,
            options.MessageId,
            options.RequestId,
            options.PartitionKey);
        return Task.CompletedTask;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("sendOptions");
    }
}

internal sealed class PublishOptionsPipe<T>(PublishOptions options) :
    IPipe<PublishContext<T>>
    where T : class
{
    public Task SendAsync(PublishContext<T> context)
    {
        OutgoingOptionsPipe.Apply(
            context,
            options.Headers,
            options.TimeToLive,
            options.CorrelationId,
            options.ConversationId,
            options.MessageId,
            options.RequestId,
            options.PartitionKey);
        return Task.CompletedTask;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("publishOptions");
    }
}

internal sealed class ScheduleOptionsPipe<T>(ScheduleOptions options) :
    IPipe<SendContext<T>>
    where T : class
{
    public Task SendAsync(SendContext<T> context)
    {
        OutgoingOptionsPipe.Apply(
            context,
            options.Headers,
            options.TimeToLive,
            options.CorrelationId,
            options.ConversationId,
            options.MessageId,
            options.RequestId,
            options.PartitionKey);
        return Task.CompletedTask;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scheduleOptions");
    }
}

static class OutgoingOptionsPipe
{
    public static void Apply(
        SendContext context,
        IReadOnlyDictionary<string, object?> headers,
        TimeSpan? timeToLive,
        Guid? correlationId,
        Guid? conversationId,
        Guid? messageId,
        Guid? requestId,
        string? partitionKey)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(headers);

        foreach ((string key, object? value) in headers)
            context.Headers.Set(key, value);

        if (timeToLive.HasValue)
            context.TimeToLive = timeToLive;
        if (correlationId.HasValue)
            context.CorrelationId = correlationId;
        if (conversationId.HasValue)
            context.ConversationId = conversationId;
        if (messageId.HasValue)
            context.MessageId = messageId;
        if (requestId.HasValue)
            context.RequestId = requestId;
        if (partitionKey is not null && context.TryGetPayload(out PartitionKeySendContext? partitionContext))
            partitionContext.PartitionKey = partitionKey;
    }
}
