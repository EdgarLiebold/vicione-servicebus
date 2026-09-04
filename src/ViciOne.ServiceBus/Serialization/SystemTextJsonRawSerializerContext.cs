using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a system text json raw serializer context implementation.
/// </summary>
public class SystemTextJsonRawSerializerContext :
    SystemTextJsonSerializerContext
{
    readonly RawSerializerOptions _rawOptions;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="objectDeserializer">The object deserializer value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="contentType">The content type value.</param>
    /// <param name="messageContext">The message context value.</param>
    /// <param name="messageTypes">The message types value.</param>
    /// <param name="rawOptions">The raw options value.</param>
    /// <param name="message">The message value.</param>
    public SystemTextJsonRawSerializerContext(IObjectDeserializer objectDeserializer, JsonSerializerOptions options, ContentType contentType,
        MessageContext messageContext, string[] messageTypes, RawSerializerOptions rawOptions, JsonElement message)
        : base(objectDeserializer, options, contentType, messageContext, messageTypes, message: message)
    {
        _rawOptions = rawOptions;
    }

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IMessageSerializer GetMessageSerializer()
    {
        return new SystemTextJsonBodyMessageSerializer(Message, ContentType, Options, _rawOptions);
    }

    /// <summary>
    /// Determines whether supported message type.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool IsSupportedMessageType<T>()
    {
        var typeUrn = MessageUrn.ForTypeString<T>();

        return _rawOptions.HasFlag(RawSerializerOptions.AnyMessageType)
            || SupportedMessageTypes.Length == 0
            || SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether supported message type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool IsSupportedMessageType(Type messageType)
    {
        var typeUrn = MessageUrn.ForTypeString(messageType);

        return _rawOptions.HasFlag(RawSerializerOptions.AnyMessageType)
            || SupportedMessageTypes.Length == 0
            || SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageTypes">The message types value.</param>
    /// <returns>The result of the operation.</returns>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return new SystemTextJsonBodyMessageSerializer(message, ContentType, Options, _rawOptions);
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
        var serializer = new SystemTextJsonBodyMessageSerializer(envelope, ContentType, Options, _rawOptions);

        serializer.Overlay(message);

        return serializer;
    }
}
