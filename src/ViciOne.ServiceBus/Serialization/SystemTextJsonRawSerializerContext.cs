using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Carries state for system text json raw serializer operations.</summary>
public class SystemTextJsonRawSerializerContext :
    SystemTextJsonSerializerContext
{
    readonly RawSerializerOptions _rawOptions;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="objectDeserializer">The object deserializer.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    /// <param name="messageContext">The message context.</param>
    /// <param name="messageTypes">The message types.</param>
    /// <param name="rawOptions">The raw options.</param>
    /// <param name="message">The message to process.</param>
    public SystemTextJsonRawSerializerContext(IObjectDeserializer objectDeserializer, JsonSerializerOptions options, ContentType contentType,
        MessageContext messageContext, string[] messageTypes, RawSerializerOptions rawOptions, JsonElement message)
        : base(objectDeserializer, options, contentType, messageContext, messageTypes, message: message)
    {
        _rawOptions = rawOptions;
    }

    /// <summary>Gets message serializer.</summary>
    /// <returns>The message serializer.</returns>
    public override IMessageSerializer GetMessageSerializer()
    {
        return new SystemTextJsonBodyMessageSerializer(Message, ContentType, Options, _rawOptions);
    }

    /// <summary>Determines whether supported message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool IsSupportedMessageType<T>()
    {
        var typeUrn = MessageUrn.ForTypeString<T>();

        return _rawOptions.HasFlag(RawSerializerOptions.AnyMessageType)
            || SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Determines whether supported message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool IsSupportedMessageType(Type messageType)
    {
        var typeUrn = MessageUrn.ForTypeString(messageType);

        return _rawOptions.HasFlag(RawSerializerOptions.AnyMessageType)
            || SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Gets message serializer.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageTypes">The message types.</param>
    /// <returns>The message serializer.</returns>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return new SystemTextJsonBodyMessageSerializer(message, ContentType, Options, _rawOptions);
    }

    /// <summary>Gets message serializer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="envelope">The envelope.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The message serializer.</returns>
    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
        var serializer = new SystemTextJsonBodyMessageSerializer(envelope, ContentType, Options, _rawOptions);

        serializer.Overlay(message);

        return serializer;
    }
}
