using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MessagePack;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

/// <summary>Exposes a decoded MessagePack envelope through the transport-neutral serializer context.</summary>
internal sealed class MessagePackSerializerContext :
    BaseSerializerContext
{
    readonly MessagePackEnvelope _envelope;

    /// <summary>Creates a serializer context for a decoded MessagePack envelope.</summary>
    /// <param name="serializer">The MessagePack serializer used for value conversion and forwarding.</param>
    /// <param name="context">The envelope-backed message metadata context.</param>
    /// <param name="supportedMessageTypes">The contract URNs represented by the encoded message.</param>
    /// <param name="envelope">The decoded envelope containing a non-null encoded message.</param>
    public MessagePackSerializerContext(MessagePackMessageSerializer serializer, MessageContext context, string[] supportedMessageTypes,
        MessagePackEnvelope envelope)
        : base(
            serializer ?? throw new ArgumentNullException(nameof(serializer)),
            context ?? throw new ArgumentNullException(nameof(context)),
            supportedMessageTypes ?? throw new ArgumentNullException(nameof(supportedMessageTypes)))
    {
        _envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));

        if (_envelope.Message is null)
            throw new ArgumentException("Message cannot be null.", nameof(envelope));
    }

    /// <summary>Attempts to decode the envelope payload as a supported message contract.</summary>
    /// <typeparam name="T">The requested message contract.</typeparam>
    /// <param name="message">Receives the decoded message when the contract is supported and decoding succeeds.</param>
    /// <returns><see langword="true"/> when a non-null <typeparamref name="T"/> was decoded; otherwise, <see langword="false"/>.</returns>
    public override bool TryGetMessage<T>([NotNullWhen(true)] out T? message)
        where T : class
    {
        if (!TryGetMessage(typeof(T), out var outMessage))
        {
            message = default;
            return false;
        }

        message = (T)outMessage!;
        return true;
    }

    /// <summary>Attempts to decode the envelope payload as a supported runtime message contract.</summary>
    /// <param name="messageType">The requested message contract type.</param>
    /// <param name="message">Receives the decoded message when the contract is supported and decoding succeeds.</param>
    /// <returns><see langword="true"/> when a non-null message was decoded; otherwise, <see langword="false"/>.</returns>
    public override bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        try
        {
            if (!IsSupportedMessageType(messageType))
            {
                message = null;
                return false;
            }

            var messagePackSerializedObjectBuffer = MessagePackMessageSerializer.GetSerializedPayloadBytes(_envelope.Message!);

            if (_envelope.IsNativeMessagePackPayload)
                message = MessagePackSerializationRuntime.Deserialize(messageType, messagePackSerializedObjectBuffer);
            else
            {
                var messageAsDictionary = MessagePackSerializationRuntime
                    .Deserialize<Dictionary<string, object>>(messagePackSerializedObjectBuffer);

                message = messageAsDictionary.Transform(messageType, ServiceBusMetadataJson.Options);
            }

            return message != default;
        }
        catch
        {
            message = default;
            return false;
        }
    }

    /// <summary>Creates a serializer that preserves this envelope while forwarding it.</summary>
    /// <returns>A MessagePack body serializer containing a private copy of the envelope payload.</returns>
    public override IMessageSerializer GetMessageSerializer()
    {
        return new MessagePackForwardingSerializer(_envelope);
    }

    /// <summary>Creates a forwarding serializer that overlays a replacement contract onto an envelope.</summary>
    /// <typeparam name="T">The replacement message contract.</typeparam>
    /// <param name="envelope">The envelope whose metadata and current payload are preserved.</param>
    /// <param name="message">The replacement values merged into the current payload by property name.</param>
    /// <returns>A MessagePack body serializer for the updated envelope.</returns>
    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(message);
        var messageEnvelopeSerializer = new MessagePackForwardingSerializer(envelope);

        messageEnvelopeSerializer.Overlay(message);

        return messageEnvelopeSerializer;
    }

    /// <summary>Creates a forwarding serializer for a new message with explicitly supplied contract URNs.</summary>
    /// <param name="message">The message encoded into the new envelope.</param>
    /// <param name="messageTypes">The contract URNs represented by the message.</param>
    /// <returns>A MessagePack body serializer for the new envelope.</returns>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageTypes);
        var messagePackEnvelope = new MessagePackEnvelope(this, message, messageTypes);

        return new MessagePackForwardingSerializer(messagePackEnvelope);
    }

    /// <summary>Projects a message into a case-insensitive property dictionary used for payload overlays.</summary>
    /// <typeparam name="T">The message contract to project.</typeparam>
    /// <param name="message">The message to project, or <see langword="null"/> for an empty dictionary.</param>
    /// <returns>A case-insensitive property dictionary.</returns>
    public override Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class
    {
        if (message is null)
            return new Dictionary<string, object>(0, StringComparer.OrdinalIgnoreCase);

        return message.Transform<Dictionary<string, object>>(ServiceBusMetadataJson.Options)
            ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
}
