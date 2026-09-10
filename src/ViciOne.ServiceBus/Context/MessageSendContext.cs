using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Context;

/// <summary>Owns the mutable transport send state for one typed message.</summary>
/// <typeparam name="TMessage">The outgoing message contract.</typeparam>
public class MessageSendContext<TMessage> :
    BasePipeContext,
    TransportSendContext<TMessage>
    where TMessage : class
{
    static readonly TimeSpanTypeConverter _timeSpanConverter = new TimeSpanTypeConverter();

    readonly Lazy<MessageBody> _body;
    readonly DictionarySendHeaders _headers;

    IMessageSerializer _serializer = null!;

    /// <summary>Creates send state and assigns time-sortable message identity metadata.</summary>
    /// <param name="message">The outgoing message.</param>
    /// <param name="cancellationToken">Cancels the send pipeline.</param>
    public MessageSendContext(TMessage message, CancellationToken cancellationToken = default)
        : base(cancellationToken)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));

        _headers = new DictionarySendHeaders();

        var messageId = NewId.Next();

        MessageId = messageId.ToGuid();
        SentTime = messageId.Timestamp;

        SupportedMessageTypes = MessageTypeCache<TMessage>.MessageTypeNames.ToArray();

        _body = new Lazy<MessageBody>(() => GetMessageBody());
    }

    /// <inheritdoc />
    public bool IsPublish { get; set; }

    /// <inheritdoc />
    public MessageBody Body => _body.Value;

    /// <inheritdoc />
    public virtual TimeSpan? Delay { get; set; }

    /// <inheritdoc />
    public Guid? MessageId { get; set; }
    /// <inheritdoc />
    public Guid? RequestId { get; set; }
    /// <inheritdoc />
    public Guid? CorrelationId { get; set; }

    /// <inheritdoc />
    public Guid? ConversationId { get; set; }
    /// <inheritdoc />
    public Guid? InitiatorId { get; set; }

    /// <inheritdoc />
    public Guid? ScheduledMessageId { get; set; }

    /// <inheritdoc />
    public SendHeaders Headers => _headers;

    /// <inheritdoc />
    public Uri? SourceAddress { get; set; }
    /// <inheritdoc />
    public Uri? DestinationAddress { get; set; }
    /// <inheritdoc />
    public Uri? ResponseAddress { get; set; }
    /// <inheritdoc />
    public Uri? FaultAddress { get; set; }

    /// <inheritdoc />
    public TimeSpan? TimeToLive { get; set; }
    /// <inheritdoc />
    public DateTimeOffset? SentTime { get; private set; }

    internal void SetDurableAdmissionMetadata(Guid idempotencyKey, Guid? correlationId)
    {
        if (_body.IsValueCreated)
            throw new InvalidOperationException("Durable admission metadata must be fixed before serialization.");

        // A repeated typed admission must serialize to the same immutable intent. The durable key therefore owns
        // the transport identities, while a null SentTime truthfully means that broker dispatch has not happened.
        MessageId = idempotencyKey;
        ConversationId = idempotencyKey;
        CorrelationId = correlationId;
        SentTime = null;
    }

    /// <inheritdoc />
    public ContentType? ContentType { get; set; }

    /// <inheritdoc />
    public IMessageSerializer Serializer
    {
        get => _serializer;
        set
        {
            if (_body.IsValueCreated)
                throw new InvalidOperationException("The message was already serialized");

            _serializer = value;
            if (_serializer != null)
                ContentType = _serializer.ContentType;
        }
    }

    /// <inheritdoc />
    public ISerialization Serialization { get; set; } = null!;
    /// <inheritdoc />
    public string[] SupportedMessageTypes { get; set; }

    /// <inheritdoc />
    public long? BodyLength => _body.IsValueCreated ? _body.Value.Length : default;

    /// <inheritdoc />
    public SendContext<T> CreateProxy<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return new SendContextProxy<T>(this, message);
    }

    /// <inheritdoc />
    public bool Durable { get; set; } = true;

    /// <inheritdoc />
    public TMessage Message { get; }

    /// <inheritdoc />
    public bool Mandatory { get; set; }

    /// <inheritdoc />
    public virtual void WritePropertiesTo(IDictionary<string, object> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (!Durable)
            properties[PropertyNames.Durable] = false;
        if (Mandatory)
            properties[PropertyNames.Mandatory] = true;
        if (Delay.HasValue)
            properties[PropertyNames.Delay] = Delay.Value;
    }

    /// <inheritdoc />
    public virtual void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        Durable = ReadBoolean(properties, PropertyNames.Durable, true);
        Mandatory = ReadBoolean(properties, PropertyNames.Mandatory);
        Delay = ReadTimeSpan(properties, PropertyNames.Delay);
    }

    MessageBody GetMessageBody()
    {
        return Serializer?.GetMessageBody(this) ?? throw new SerializationException("Unable to serialize message, no serializer specified.");
    }

    /// <summary>Reads a UTF-8 or native string value from a transport property bag.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or has an unsupported representation.</param>
    /// <returns>The decoded string, or <paramref name="defaultValue" /> when no supported value is available.</returns>
    protected static string? ReadString(IReadOnlyDictionary<string, object> properties, string key, string? defaultValue = null)
    {
        if (properties.TryGetValue(key, out var value))
        {
            if (value is string text)
                return text;

            if (value is byte[] bytes)
            {
                text = Encoding.UTF8.GetString(bytes);
                return text;
            }
        }

        return defaultValue;
    }

    /// <summary>Reads a semicolon-delimited UTF-8 or native string array from a transport property bag.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <returns>The decoded entries, or an empty array when no supported value is available.</returns>
    protected static string[] ReadStringArray(IReadOnlyDictionary<string, object> properties, string key)
    {
        if (properties.TryGetValue(key, out var value))
        {
            if (value is string text)
                return text.Split(';');

            if (value is byte[] bytes)
            {
                text = Encoding.UTF8.GetString(bytes);
                return text.Split(';');
            }
        }

        return [];
    }

    /// <summary>Reads a string-encoded duration from a transport property bag.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or invalid.</param>
    /// <returns>The parsed duration, or <paramref name="defaultValue" /> when parsing is not possible.</returns>
    protected static TimeSpan? ReadTimeSpan(IReadOnlyDictionary<string, object> properties, string key, TimeSpan? defaultValue = null)
    {
        if (properties.TryGetValue(key, out var storedValue) && storedValue is TimeSpan duration)
            return duration;

        var value = ReadString(properties, key);

        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return _timeSpanConverter.TryConvert(value, out var result) ? result : defaultValue;
    }

    /// <summary>Reads a case-insensitive enum name from a transport property bag.</summary>
    /// <typeparam name="T">The enum value type.</typeparam>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or invalid.</param>
    /// <returns>The parsed enum value, or <paramref name="defaultValue" /> when parsing is not possible.</returns>
    protected static T? ReadEnum<T>(IReadOnlyDictionary<string, object> properties, string key, T? defaultValue = default)
        where T : struct
    {
        if (properties.TryGetValue(key, out var value))
        {
            if (value is string text)
                return Enum.TryParse<T>(text, true, out var enumValue) ? enumValue : defaultValue;
        }

        return defaultValue;
    }

    /// <summary>Reads a byte-compatible integer from a transport property bag.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or invalid.</param>
    /// <returns>The byte value, or <paramref name="defaultValue" /> when no compatible value is available.</returns>
    /// <exception cref="OverflowException">The stored integer cannot be represented as a byte.</exception>
    protected static byte ReadByte(IReadOnlyDictionary<string, object> properties, string key, byte defaultValue = default)
    {
        if (!properties.TryGetValue(key, out var value))
            return defaultValue;

        if (value is byte byteValue)
            return byteValue;

        var longValue = ReadLong(properties, key);

        return longValue.HasValue ? checked((byte)longValue.Value) : defaultValue;
    }

    /// <summary>Reads a 32-bit integer from a transport property bag.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or invalid.</param>
    /// <returns>The integer value, or <paramref name="defaultValue" /> when no compatible value is available.</returns>
    /// <exception cref="OverflowException">The stored integer cannot be represented as a 32-bit integer.</exception>
    protected static int? ReadInt(IReadOnlyDictionary<string, object> properties, string key, int? defaultValue = null)
    {
        var longValue = ReadLong(properties, key);

        return longValue.HasValue ? checked((int)longValue.Value) : defaultValue;
    }

    /// <summary>Reads a 16-bit integer from a transport property bag.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or invalid.</param>
    /// <returns>The integer value, or <paramref name="defaultValue" /> when no compatible value is available.</returns>
    /// <exception cref="OverflowException">The stored integer cannot be represented as a 16-bit integer.</exception>
    protected static short? ReadShort(IReadOnlyDictionary<string, object> properties, string key, short? defaultValue = null)
    {
        var longValue = ReadLong(properties, key);

        return longValue.HasValue ? checked((short)longValue.Value) : defaultValue;
    }

    /// <summary>Reads a 64-bit integer from a native integer, character, UTF-8, or string property value.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or invalid.</param>
    /// <returns>The integer value, or <paramref name="defaultValue" /> when no compatible value is available.</returns>
    protected static long? ReadLong(IReadOnlyDictionary<string, object> properties, string key, long? defaultValue = null)
    {
        if (properties.TryGetValue(key, out var value))
        {
            if (value is long longValue)
                return longValue;

            switch (value)
            {
                case int intValue:
                    return intValue;
                case short shortValue:
                    return shortValue;
                case char charValue:
                    return charValue;
            }

            if (value is string text)
                return long.TryParse(text, out longValue) ? longValue : defaultValue;

            if (value is byte[] bytes)
            {
                text = Encoding.UTF8.GetString(bytes);
                return long.TryParse(text, out longValue) ? longValue : defaultValue;
            }
        }

        return defaultValue;
    }

    /// <summary>Reads a native Boolean or zero/nonzero integer from a transport property bag.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or invalid.</param>
    /// <returns>The decoded Boolean value, or <paramref name="defaultValue" /> when no compatible value is available.</returns>
    protected static bool ReadBoolean(IReadOnlyDictionary<string, object> properties, string key, bool defaultValue = default)
    {
        if (!properties.TryGetValue(key, out var value))
            return defaultValue;

        if (value is bool boolValue)
            return boolValue;

        var longValue = ReadLong(properties, key);

        return longValue.HasValue ? longValue.Value != 0 : defaultValue;
    }


    static class PropertyNames
    {
        public const string Delay = "Delay";
        public const string Durable = "Durable";
        public const string Mandatory = "Mandatory";
    }
}
