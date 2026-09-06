using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Carries state for system text json serializer operations.</summary>
public class SystemTextJsonSerializerContext :
    BaseSerializerContext
{
    readonly MessageEnvelope? _envelope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="objectDeserializer">The object deserializer.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    /// <param name="messageContext">The message context.</param>
    /// <param name="messageTypes">The message types.</param>
    /// <param name="envelope">The envelope.</param>
    /// <param name="message">The message to process.</param>
    public SystemTextJsonSerializerContext(IObjectDeserializer objectDeserializer, JsonSerializerOptions options, ContentType contentType,
        MessageContext messageContext, string[] messageTypes, MessageEnvelope? envelope = null, object? message = null)
        : base(objectDeserializer, messageContext, messageTypes)
    {
        _envelope = envelope;
        ContentType = contentType;
        Message = message ?? envelope?.Message ?? throw new ArgumentNullException(nameof(envelope));
        Options = options;
    }

    /// <summary>Gets the message.</summary>
    protected object Message { get; }
    /// <summary>Gets the content type.</summary>
    protected ContentType ContentType { get; }
    /// <summary>Gets the options.</summary>
    protected JsonSerializerOptions Options { get; }

    /// <summary>Attempts to get message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">Receives the message produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage<T>([NotNullWhen(true)] out T? message)
        where T : class
    {
        var jsonElement = GetJsonElement(Message);

        if (typeof(T) == typeof(JsonObject))
        {
            message = JsonObject.Create(jsonElement) as T;
            return message != null;
        }

        if (IsSupportedMessageType<T>())
        {
            if (Message is T messageOfT)
            {
                message = messageOfT;
                return true;
            }

            message = jsonElement.Deserialize<T>(Options);
            return message != null;
        }

        message = null;
        return false;
    }

    /// <summary>Attempts to get message.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">Receives the message produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (!IsSupportedMessageType(messageType))
        {
            message = null;
            return false;
        }

        var jsonElement = GetJsonElement(Message);

        message = jsonElement.Deserialize(messageType, Options);

        return message != null;
    }

    /// <summary>Gets message serializer.</summary>
    /// <returns>The message serializer.</returns>
    public override IMessageSerializer GetMessageSerializer()
    {
        if (_envelope == null)
            throw new InvalidOperationException("This should be overloaded");

        return new SystemTextJsonBodyMessageSerializer(_envelope, ContentType, Options);
    }

    /// <summary>Gets message serializer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="envelope">The envelope.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The message serializer.</returns>
    public override IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
    {
        var serializer = new SystemTextJsonBodyMessageSerializer(envelope, ContentType, Options);

        serializer.Overlay(message);

        return serializer;
    }

    /// <summary>Gets message serializer.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageTypes">The message types.</param>
    /// <returns>The message serializer.</returns>
    public override IMessageSerializer GetMessageSerializer(object message, string[] messageTypes)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var envelope = new JsonMessageEnvelope(this, message, messageTypes);

        return new SystemTextJsonBodyMessageSerializer(envelope, ContentType, Options, messageTypes);
    }

    /// <summary>Converts this value to dictionary.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>The converted dictionary.</returns>
    public override Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class
    {
        return message == null
            ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            : JsonSerializer.SerializeToElement(message, Options).Deserialize<Dictionary<string, object>>()!;
    }

    static JsonElement GetJsonElement(object message)
    {
        return message is JsonElement element
            ? element.ValueKind == JsonValueKind.Null
                ? new JsonElement()
                : element
            : new JsonElement();
    }
}
