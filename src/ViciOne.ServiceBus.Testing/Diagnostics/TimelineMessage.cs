using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Testing;

sealed class TimelineMessage
{
    public TimelineMessage(IPublishedMessage message)
        : this(message?.Context ?? throw new ArgumentNullException(nameof(message)))
    {
        MessageType = message.MessageType;
        ShortTypeName = message.ShortTypeName;
        EventType = "Publish";
        StartTime = message.StartTime;
        ElapsedTime = message.ElapsedTime;
    }

    public TimelineMessage(ISentMessage message)
        : this(message?.Context ?? throw new ArgumentNullException(nameof(message)))
    {
        MessageType = message.MessageType;
        ShortTypeName = message.ShortTypeName;
        EventType = "Send";
        StartTime = message.StartTime;
        ElapsedTime = message.ElapsedTime;
    }

    public TimelineMessage(IConsumedMessage message)
        : this(message?.Context ?? throw new ArgumentNullException(nameof(message)))
    {
        MessageType = message.MessageType;
        ShortTypeName = message.ShortTypeName;
        EventType = "Consume";
        StartTime = message.StartTime;
        ElapsedTime = message.ElapsedTime;
    }

    TimelineMessage(SendContext context)
    {
        MessageId = context.MessageId;
        ConversationKey = context.ConversationId ?? context.MessageId ?? context.CorrelationId ?? NewId.NextGuid();
        Address = context.DestinationAddress.GetEndpointName();

        if (context.TryGetPayload(out ConsumeContext? consumeContext))
            ParentMessageId = consumeContext.MessageId;
    }

    TimelineMessage(ConsumeContext context)
    {
        MessageId = context.MessageId;
        ConversationKey = context.ConversationId ?? context.MessageId ?? context.CorrelationId ?? NewId.NextGuid();
        Address = context.ReceiveContext.InputAddress.GetEndpointName();
    }

    public DateTimeOffset StartTime { get; }
    public TimeSpan? ElapsedTime { get; }
    public Guid? MessageId { get; }
    public Guid ConversationKey { get; }
    public Type MessageType { get; } = typeof(object);
    public string ShortTypeName { get; } = string.Empty;
    public string EventType { get; } = string.Empty;
    public Guid? ParentMessageId { get; }
    public string? Address { get; }
}
