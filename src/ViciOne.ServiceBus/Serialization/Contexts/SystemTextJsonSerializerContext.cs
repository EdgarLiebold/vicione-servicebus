using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Materializes declared message contracts from an envelope-encoded JSON value.</summary>
internal class SystemTextJsonSerializerContext :
    BaseSerializerContext
{
    readonly MessageEnvelope? _envelope;

    /// <summary>Creates a JSON serializer context for one deserialized message.</summary>
    /// <param name="objectDeserializer">The converter for header and metadata values.</param>
    /// <param name="options">The immutable JSON serializer options.</param>
    /// <param name="contentType">The media type of the encoded message.</param>
    /// <param name="messageContext">The deserialized message metadata.</param>
    /// <param name="messageTypes">The declared message contract URNs.</param>
    /// <param name="envelope">The original envelope, when the encoded format has one.</param>
    /// <param name="message">The raw message value, when it is supplied independently of an envelope.</param>
    public SystemTextJsonSerializerContext(IObjectDeserializer objectDeserializer, JsonSerializerOptions options, ContentType contentType,
        MessageContext messageContext, string[] messageTypes, MessageEnvelope? envelope = null, object? message = null)
        : base(objectDeserializer, messageContext, messageTypes)
    {
        _envelope = envelope;
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        Message = message ?? envelope?.Message ?? throw new ArgumentNullException(nameof(envelope));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Gets the deserialized JSON message representation.</summary>
    protected object Message { get; }
    /// <summary>Gets the media type of the encoded message.</summary>
    protected ContentType ContentType { get; }
    /// <summary>Gets the JSON serializer options used for this message.</summary>
    protected JsonSerializerOptions Options { get; }

    /// <summary>Tries to materialize the message as a declared contract.</summary>
    /// <typeparam name="T">The requested message contract.</typeparam>
    /// <param name="message">The materialized message when conversion succeeds.</param>
    /// <returns><see langword="true" /> when the declared contract can be materialized; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage<T>([NotNullWhen(true)] out T? message)
        where T : class
    {
        if (typeof(T) == typeof(JsonObject))
        {
            message = JsonObject.Create(GetJsonElement(Message)) as T;
            return message != null;
        }

        if (IsSupportedMessageType<T>())
        {
            if (Message is T messageOfT)
            {
                message = messageOfT;
                return true;
            }

            message = GetJsonElement(Message).Deserialize<T>(Options);
            return message != null;
        }

        message = null;
        return false;
    }

    /// <summary>Tries to materialize the message as a declared runtime contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">The materialized message when conversion succeeds.</param>
    /// <returns><see langword="true" /> when the declared contract can be materialized; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (!IsSupportedMessageType(messageType))
        {
            message = null;
            return false;
        }

        if (messageType.IsInstanceOfType(Message))
        {
            message = Message;
            return true;
        }

        message = GetJsonElement(Message).Deserialize(messageType, Options);

        return message != null;
    }

    /// <summary>Creates a serializer that preserves the current envelope.</summary>
    /// <returns>A serializer for forwarding the current envelope.</returns>
    public override IMessageSerializer GetMessageSerializer()
    {
        if (_envelope == null)
            throw new InvalidOperationException("The current raw JSON context has no message envelope to preserve.");

        return new SystemTextJsonForwardingSerializer(_envelope, ContentType, Options);
    }

    /// <summary>Creates a serializer that overlays a typed message on an existing envelope.</summary>
    /// <typeparam name="T">The replacement message contract.</typeparam>
    /// <param name="envelope">The envelope metadata to preserve.</param>
    /// <param name="message">The replacement message.</param>
    /// <returns>A serializer for the updated envelope.</returns>
    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(message);
        var serializer = new SystemTextJsonForwardingSerializer(envelope, ContentType, Options);

        serializer.Overlay(message);

        return serializer;
    }

    /// <summary>Creates an envelope serializer for a replacement message and explicit contract set.</summary>
    /// <param name="message">The replacement message.</param>
    /// <param name="messageTypes">The replacement message contract URNs.</param>
    /// <returns>A serializer for the replacement message.</returns>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageTypes);

        var envelope = new JsonMessageEnvelope(this, message, messageTypes);

        return new SystemTextJsonForwardingSerializer(envelope, ContentType, Options, messageTypes);
    }

    /// <summary>Projects a message into a case-insensitive JSON property dictionary.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The message to project.</param>
    /// <returns>The projected properties, or an empty dictionary for <see langword="null" />.</returns>
    public override Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class
    {
        return message == null
            ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            : JsonSerializer.SerializeToElement(message, Options).Deserialize<Dictionary<string, object>>(Options)
                ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    JsonElement GetJsonElement(object message)
    {
        return message is JsonElement element
            ? element
            : JsonSerializer.SerializeToElement(message, message.GetType(), Options);
    }
}
