using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MessagePack;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a message pack message serializer context implementation.
/// </summary>
public class MessagePackMessageSerializerContext :
    BaseSerializerContext
{
    readonly MessagePackEnvelope _envelope;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serializer">The serializer value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="supportedMessageTypes">The supported message types value.</param>
    /// <param name="envelope">The envelope value.</param>
    public MessagePackMessageSerializerContext(MessagePackMessageSerializer serializer, MessageContext context, string[] supportedMessageTypes,
        MessagePackEnvelope envelope)
        : base(serializer, context, supportedMessageTypes)
    {
        _envelope = envelope;

        if (_envelope.Message is null)
            throw new ArgumentException("Message cannot be null.", nameof(envelope));
    }

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="message">The message value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message)
    {
        try
        {
            if (!IsSupportedMessageType(messageType))
            {
                message = null;
                return false;
            }

            var messagePackSerializedObjectBuffer = MessagePackMessageSerializer.EnsureObjectBufferFormatIsByteArray(_envelope.Message!);

            if (_envelope.IsMessageNativeMessagePackSerialized)
                message = InternalMessagePackResolver.Deserialize(messageType, messagePackSerializedObjectBuffer);
            else
            {
                // If a message is serialized as dictionary of string-object pairs, we need to deserialize using a different approach.

                var messageAsDictionary = InternalMessagePackResolver
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

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IMessageSerializer GetMessageSerializer()
    {
        if (_envelope is null)
            throw new InvalidOperationException("Context has no envelope.");

        return new MessagePackMessageBodySerializer(_envelope);
    }

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="envelope">The envelope value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
        var messageEnvelopeSerializer = new MessagePackMessageBodySerializer(envelope);

        messageEnvelopeSerializer.OverrideMessage(message);

        return messageEnvelopeSerializer;
    }

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageTypes">The message types value.</param>
    /// <returns>The result of the operation.</returns>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        var messagePackEnvelope = new MessagePackEnvelope(this, message, messageTypes);

        return new MessagePackMessageBodySerializer(messagePackEnvelope);
    }

    /// <summary>
    /// Performs the to dictionary operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class
    {
        if (message is null)
            return new Dictionary<string, object>(0, StringComparer.OrdinalIgnoreCase);

        // We serialize internally using JSON.
        return message.Transform<Dictionary<string, object>>(ServiceBusMetadataJson.Options)
            ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
}
