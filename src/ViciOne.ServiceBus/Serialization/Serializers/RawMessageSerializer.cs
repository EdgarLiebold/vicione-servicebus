using System.Text.Json;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Projects ServiceBus transport metadata into headers for raw message formats.</summary>
public abstract class RawMessageSerializer
{
    /// <summary>Writes the current send metadata as transport headers.</summary>
    /// <param name="context">The outgoing message context.</param>
    protected virtual void SetRawMessageHeaders(SendContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.MessageId.HasValue)
            context.Headers.Set(MessageHeaders.MessageId, context.MessageId.Value.ToString("D"));

        if (context.CorrelationId.HasValue)
            context.Headers.Set(MessageHeaders.CorrelationId, context.CorrelationId.Value.ToString("D"));

        if (context.ConversationId.HasValue)
            context.Headers.Set(MessageHeaders.ConversationId, context.ConversationId.Value.ToString("D"));

        if (context.InitiatorId.HasValue)
            context.Headers.Set(MessageHeaders.InitiatorId, context.InitiatorId.Value.ToString("D"));

        if (context.RequestId.HasValue)
            context.Headers.Set(MessageHeaders.RequestId, context.RequestId.Value.ToString("D"));

        if (context.SupportedMessageTypes?.Length > 0)
            context.Headers.Set(MessageHeaders.MessageType, string.Join(";", context.SupportedMessageTypes));

        if (context.ResponseAddress != null)
            context.Headers.Set(MessageHeaders.ResponseAddress, context.ResponseAddress);

        if (context.FaultAddress != null)
            context.Headers.Set(MessageHeaders.FaultAddress, context.FaultAddress);

        if (context.SourceAddress != null)
            context.Headers.Set(MessageHeaders.SourceAddress, context.SourceAddress);

        context.Headers.Set(MessageHeaders.Host.Info, JsonSerializer.Serialize(HostMetadataCache.Host, ServiceBusMetadataJson.Options));
    }
}
