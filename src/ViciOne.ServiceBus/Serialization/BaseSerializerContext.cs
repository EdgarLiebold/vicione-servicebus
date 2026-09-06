using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Carries state for base serializer operations.</summary>
public abstract class BaseSerializerContext :
    SerializerContext
{
    readonly MessageContext _context;
    readonly IObjectDeserializer _deserializer;

    Guid? _conversationId;
    Guid? _correlationId;
    Uri? _destinationAddress;
    Uri? _faultAddress;
    Headers? _headers;
    Guid? _initiatorId;
    Guid? _messageId;
    Guid? _requestId;
    Uri? _responseAddress;
    Uri? _sourceAddress;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="deserializer">The deserializer.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="supportedMessageTypes">The supported message types.</param>
    protected BaseSerializerContext(IObjectDeserializer deserializer, MessageContext context, string[] supportedMessageTypes)
    {
        _context = context;
        _deserializer = deserializer;

        SupportedMessageTypes = supportedMessageTypes;

    }

    /// <summary>Gets the message id.</summary>
    public Guid? MessageId => _messageId ??= _context.MessageId;
    /// <summary>Gets the request id.</summary>
    public Guid? RequestId => _requestId ??= _context.RequestId;
    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId => _correlationId ??= _context.CorrelationId;
    /// <summary>Gets the conversation id.</summary>
    public Guid? ConversationId => _conversationId ??= _context.ConversationId;
    /// <summary>Gets the initiator id.</summary>
    public Guid? InitiatorId => _initiatorId ??= _context.InitiatorId;
    /// <summary>Gets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime => _context.ExpirationTime;
    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => _sourceAddress ??= _context.SourceAddress;
    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress => _destinationAddress ??= _context.DestinationAddress;
    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => _responseAddress ??= _context.ResponseAddress;
    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => _faultAddress ??= _context.FaultAddress;
    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => _context.SentTime;
    /// <summary>Gets the headers.</summary>
    public Headers Headers => _headers ??= _context.Headers;
    /// <summary>Gets the host.</summary>
    public HostInfo Host => _context.Host;

    /// <summary>Gets the supported message types.</summary>
    public string[] SupportedMessageTypes { get; }

    /// <summary>Deserializes object.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The deserialized object.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = default)
        where T : class
    {
        return _deserializer.DeserializeObject(value, defaultValue);
    }

    /// <summary>Deserializes object.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The deserialized object.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : struct
    {
        return _deserializer.DeserializeObject(value, defaultValue);
    }

    /// <summary>Serializes object.</summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The serialized object.</returns>
    public MessageBody SerializeObject(object? value)
    {
        return _deserializer.SerializeObject(value);
    }

    /// <summary>Attempts to get message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">Receives the message produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetMessage<T>([NotNullWhen(true)] out T? message)
        where T : class;

    /// <summary>Attempts to get message.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">Receives the message produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message);

    /// <summary>Gets message serializer.</summary>
    /// <returns>The message serializer.</returns>
    public abstract IMessageSerializer GetMessageSerializer();

    /// <summary>Gets message serializer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="envelope">The envelope.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The message serializer.</returns>
    public abstract IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
        where T : class;

    /// <summary>Gets message serializer.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageTypes">The message types.</param>
    /// <returns>The message serializer.</returns>
    public abstract IMessageSerializer GetMessageSerializer(object message, string[] messageTypes);

    /// <summary>Converts this value to dictionary.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>The converted dictionary.</returns>
    public abstract Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class;

    /// <summary>Determines whether supported message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool IsSupportedMessageType<T>()
        where T : class
    {
        var typeUrn = MessageUrn.ForTypeString<T>();

        return SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Determines whether supported message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool IsSupportedMessageType(Type messageType)
    {
        var typeUrn = MessageUrn.ForTypeString(messageType);

        return SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }
}
