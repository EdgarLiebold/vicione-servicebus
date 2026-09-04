using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a base serializer context implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="deserializer">The deserializer value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="supportedMessageTypes">The supported message types value.</param>
    protected BaseSerializerContext(IObjectDeserializer deserializer, MessageContext context, string[] supportedMessageTypes)
    {
        _context = context;
        _deserializer = deserializer;

        SupportedMessageTypes = supportedMessageTypes;

    }

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public Guid? MessageId => _messageId ??= _context.MessageId;
    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public Guid? RequestId => _requestId ??= _context.RequestId;
    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public Guid? CorrelationId => _correlationId ??= _context.CorrelationId;
    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    public Guid? ConversationId => _conversationId ??= _context.ConversationId;
    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    public Guid? InitiatorId => _initiatorId ??= _context.InitiatorId;
    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime => _context.ExpirationTime;
    /// <summary>
    /// Gets the source address value.
    /// </summary>
    public Uri? SourceAddress => _sourceAddress ??= _context.SourceAddress;
    /// <summary>
    /// Gets the destination address value.
    /// </summary>
    public Uri? DestinationAddress => _destinationAddress ??= _context.DestinationAddress;
    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public Uri? ResponseAddress => _responseAddress ??= _context.ResponseAddress;
    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    public Uri? FaultAddress => _faultAddress ??= _context.FaultAddress;
    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    public DateTimeOffset? SentTime => _context.SentTime;
    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public Headers Headers => _headers ??= _context.Headers;
    /// <summary>
    /// Gets the host value.
    /// </summary>
    public HostInfo Host => _context.Host;

    /// <summary>
    /// Gets the supported message types value.
    /// </summary>
    public string[] SupportedMessageTypes { get; }

    /// <summary>
    /// Performs the deserialize object operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = default)
        where T : class
    {
        return _deserializer.DeserializeObject(value, defaultValue);
    }

    /// <summary>
    /// Performs the deserialize object operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : struct
    {
        return _deserializer.DeserializeObject(value, defaultValue);
    }

    /// <summary>
    /// Performs the serialize object operation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public MessageBody SerializeObject(object? value)
    {
        return _deserializer.SerializeObject(value);
    }

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetMessage<T>([NotNullWhen(true)] out T? message)
        where T : class;

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="message">The message value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message);

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public abstract IMessageSerializer GetMessageSerializer();

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="envelope">The envelope value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public abstract IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
        where T : class;

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageTypes">The message types value.</param>
    /// <returns>The result of the operation.</returns>
    public abstract IMessageSerializer GetMessageSerializer(object message, string[] messageTypes);

    /// <summary>
    /// Performs the to dictionary operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class;

    /// <summary>
    /// Determines whether supported message type.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool IsSupportedMessageType<T>()
        where T : class
    {
        var typeUrn = MessageUrn.ForTypeString<T>();

        return SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether supported message type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool IsSupportedMessageType(Type messageType)
    {
        var typeUrn = MessageUrn.ForTypeString(messageType);

        return SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }
}
