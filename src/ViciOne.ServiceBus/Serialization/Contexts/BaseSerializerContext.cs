using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Provides shared metadata projection and object conversion for serializer contexts.</summary>
public abstract class BaseSerializerContext :
    SerializerContext
{
    readonly MessageContext _context;
    readonly IObjectDeserializer _deserializer;
    readonly string[] _supportedMessageTypes;

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

    /// <summary>Creates a serializer context over deserialized message metadata.</summary>
    /// <param name="deserializer">The converter for header and metadata values.</param>
    /// <param name="context">The deserialized message metadata.</param>
    /// <param name="supportedMessageTypes">The declared message contract URNs.</param>
    protected BaseSerializerContext(IObjectDeserializer deserializer, MessageContext context, string[] supportedMessageTypes)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        ArgumentNullException.ThrowIfNull(supportedMessageTypes);
        if (supportedMessageTypes.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Message type names cannot be null, empty, or white space.", nameof(supportedMessageTypes));
        _supportedMessageTypes = supportedMessageTypes.ToArray();
    }

    /// <summary>Gets the message identifier.</summary>
    public Guid? MessageId => _messageId ??= _context.MessageId;
    /// <summary>Gets the request identifier.</summary>
    public Guid? RequestId => _requestId ??= _context.RequestId;
    /// <summary>Gets the correlation identifier.</summary>
    public Guid? CorrelationId => _correlationId ??= _context.CorrelationId;
    /// <summary>Gets the conversation identifier.</summary>
    public Guid? ConversationId => _conversationId ??= _context.ConversationId;
    /// <summary>Gets the initiating message identifier.</summary>
    public Guid? InitiatorId => _initiatorId ??= _context.InitiatorId;
    /// <summary>Gets the absolute message expiration time.</summary>
    public DateTimeOffset? ExpirationTime => _context.ExpirationTime;
    /// <summary>Gets the logical source endpoint address.</summary>
    public Uri? SourceAddress => _sourceAddress ??= _context.SourceAddress;
    /// <summary>Gets the destination endpoint address.</summary>
    public Uri? DestinationAddress => _destinationAddress ??= _context.DestinationAddress;
    /// <summary>Gets the response endpoint address.</summary>
    public Uri? ResponseAddress => _responseAddress ??= _context.ResponseAddress;
    /// <summary>Gets the fault endpoint address.</summary>
    public Uri? FaultAddress => _faultAddress ??= _context.FaultAddress;
    /// <summary>Gets the envelope creation time.</summary>
    public DateTimeOffset? SentTime => _context.SentTime;
    /// <summary>Gets the application headers.</summary>
    public Headers Headers => _headers ??= _context.Headers;
    /// <summary>Gets metadata that identifies the sending host.</summary>
    public HostInfo Host => _context.Host;

    /// <summary>Gets an independent copy of the declared message contract URNs.</summary>
    public string[] SupportedMessageTypes => _supportedMessageTypes.ToArray();

    /// <summary>Converts a metadata value to a reference type.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="value">The serialized or native value.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The converted value or <paramref name="defaultValue" />.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = default)
        where T : class
    {
        return _deserializer.DeserializeObject(value, defaultValue);
    }

    /// <summary>Converts a metadata value to a value type.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="value">The serialized or native value.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The converted value or <paramref name="defaultValue" />.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : struct
    {
        return _deserializer.DeserializeObject(value, defaultValue);
    }

    /// <summary>Serializes a metadata value with the current message codec.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The encoded metadata body.</returns>
    public MessageBody SerializeObject(object? value)
    {
        return _deserializer.SerializeObject(value);
    }

    /// <summary>Tries to deserialize the body as a declared message contract.</summary>
    /// <typeparam name="T">The requested message contract.</typeparam>
    /// <param name="message">The message when the contract is declared and conversion succeeds.</param>
    /// <returns><see langword="true" /> when the message is available as <typeparamref name="T" />; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetMessage<T>([NotNullWhen(true)] out T? message)
        where T : class;

    /// <summary>Tries to deserialize the body as a declared runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">The message when the contract is declared and conversion succeeds.</param>
    /// <returns><see langword="true" /> when the message is available as <paramref name="messageType" />; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message);

    /// <summary>Creates a serializer that preserves the current message and envelope.</summary>
    /// <returns>A serializer for forwarding the current message.</returns>
    public abstract IMessageSerializer GetMessageSerializer();

    /// <summary>Creates a serializer that overlays a typed message on an existing envelope.</summary>
    /// <typeparam name="T">The replacement message contract.</typeparam>
    /// <param name="envelope">The metadata envelope to preserve.</param>
    /// <param name="message">The replacement message.</param>
    /// <returns>A serializer for the updated envelope.</returns>
    public abstract IMessageSerializer GetMessageSerializer<T>(MessageEnvelope envelope, T message)
        where T : class;

    /// <summary>Creates a serializer for a replacement message and explicit contract set.</summary>
    /// <param name="message">The replacement message.</param>
    /// <param name="messageTypes">The replacement message contract URNs.</param>
    /// <returns>A serializer for the replacement message.</returns>
    public abstract IMessageSerializer GetMessageSerializer(object message, string[] messageTypes);

    /// <summary>Projects a message into a case-insensitive property dictionary.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The message to project.</param>
    /// <returns>The projected message properties.</returns>
    public abstract Dictionary<string, object> ToDictionary<T>(T? message)
        where T : class;

    /// <summary>Determines whether a message contract was declared by the serialized body.</summary>
    /// <typeparam name="T">The candidate message contract.</typeparam>
    /// <returns><see langword="true" /> when the contract URN is declared; otherwise, <see langword="false" />.</returns>
    public virtual bool IsSupportedMessageType<T>()
        where T : class
    {
        var typeUrn = MessageUrn.ForTypeString<T>();

        return _supportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Determines whether a runtime message contract was declared by the serialized body.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the contract URN is declared; otherwise, <see langword="false" />.</returns>
    public virtual bool IsSupportedMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        var typeUrn = MessageUrn.ForTypeString(messageType);

        return _supportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }
}
