using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Deserializes and forwards a raw JSON body while enforcing its declared message-type policy.</summary>
internal sealed class SystemTextJsonRawSerializerContext :
    SystemTextJsonSerializerContext
{
    readonly RawSerializerOptions _rawOptions;

    /// <summary>Creates a serializer context for a raw JSON message.</summary>
    /// <param name="objectDeserializer">The converter for header and metadata values.</param>
    /// <param name="options">The immutable JSON serializer options.</param>
    /// <param name="contentType">The raw JSON media type.</param>
    /// <param name="messageContext">The metadata projected from transport headers.</param>
    /// <param name="messageTypes">The declared message contract URNs.</param>
    /// <param name="rawOptions">The raw message-type and header policy.</param>
    /// <param name="message">The raw JSON value.</param>
    public SystemTextJsonRawSerializerContext(IObjectDeserializer objectDeserializer, JsonSerializerOptions options, ContentType contentType,
        MessageContext messageContext, string[] messageTypes, RawSerializerOptions rawOptions, JsonElement message)
        : base(objectDeserializer, options, contentType, messageContext, messageTypes, message: message)
    {
        if ((rawOptions & ~RawSerializerOptions.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(rawOptions), rawOptions, "The raw serializer options contain unsupported flags.");
        _rawOptions = rawOptions;
    }

    /// <summary>Creates a raw serializer for the current message.</summary>
    /// <returns>A serializer that preserves the current raw JSON body.</returns>
    public override IMessageSerializer GetMessageSerializer()
    {
        return new SystemTextJsonForwardingSerializer(Message, ContentType, Options, _rawOptions, SupportedMessageTypes.Length > 0 ? SupportedMessageTypes : null);
    }

    /// <summary>Determines whether a message contract is declared or unrestricted by policy.</summary>
    /// <typeparam name="T">The candidate message contract.</typeparam>
    /// <returns><see langword="true" /> when the contract is accepted; otherwise, <see langword="false" />.</returns>
    public override bool IsSupportedMessageType<T>()
    {
        var typeUrn = MessageUrn.ForTypeString<T>();

        return _rawOptions.HasFlag(RawSerializerOptions.AnyMessageType)
            || SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Determines whether a runtime message contract is declared or unrestricted by policy.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the contract is accepted; otherwise, <see langword="false" />.</returns>
    public override bool IsSupportedMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        var typeUrn = MessageUrn.ForTypeString(messageType);

        return _rawOptions.HasFlag(RawSerializerOptions.AnyMessageType)
            || SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Creates a raw serializer for a replacement message and explicit contract set.</summary>
    /// <param name="message">The replacement message.</param>
    /// <param name="messageTypes">The replacement message contract URNs.</param>
    /// <returns>A serializer for the replacement raw body.</returns>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageTypes);

        return new SystemTextJsonForwardingSerializer(message, ContentType, Options, _rawOptions, messageTypes);
    }

    /// <summary>Creates a raw serializer for a typed message extracted from an existing envelope.</summary>
    /// <typeparam name="T">The replacement message contract.</typeparam>
    /// <param name="envelope">The source envelope metadata.</param>
    /// <param name="message">The replacement message.</param>
    /// <returns>A serializer for the replacement raw body.</returns>
    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(message);
        var serializer = new SystemTextJsonForwardingSerializer(envelope, ContentType, Options, _rawOptions);

        serializer.Overlay(message);

        return serializer;
    }
}
