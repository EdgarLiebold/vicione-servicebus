using System;
using System.Collections.Generic;
using System.Net.Mime;
using MessagePack;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

internal sealed class MessagePackMessageBodySerializer :
    IMessageSerializer
{
    public ContentType ContentType { get; } = MessagePackMessageSerializer.MessagePackContentType;

    readonly MessagePackEnvelope _envelope;

    public MessagePackMessageBodySerializer(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        _envelope = new MessagePackEnvelope(envelope);
    }

    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        _envelope.Update(context);

        if (_envelope.MessageType != null)
            context.SupportedMessageTypes = _envelope.MessageType;

        return new MessagePackMessageBody<T>(context, _envelope);
    }

    public void OverrideMessage<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        Dictionary<string, object> currentMessage;

        if (_envelope.Message is not null)
        {
            currentMessage = InternalMessagePackResolver
                .Deserialize<Dictionary<string, object>>((byte[])_envelope.Message);
        }
        else
            currentMessage = new Dictionary<string, object>(0);

        currentMessage = new Dictionary<string, object>(currentMessage, StringComparer.OrdinalIgnoreCase);
        var messageToMerge = message
            .Transform<Dictionary<string, object>>(ServiceBusMetadataJson.Options);

        if (messageToMerge is null)
            return;

        foreach (KeyValuePair<string, object> overlay in messageToMerge)
            currentMessage[overlay.Key] = overlay.Value;

        _envelope.IsMessageNativeMessagePackSerialized = false;
        _envelope.Message = InternalMessagePackResolver.Serialize(currentMessage);
    }
}
