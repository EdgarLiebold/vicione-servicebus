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

/// <summary>Carries state for message send operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageSendContext<TMessage> :
    BasePipeContext,
    TransportSendContext<TMessage>
    where TMessage : class
{
    static readonly TimeSpanTypeConverter _timeSpanConverter = new TimeSpanTypeConverter();

    readonly Lazy<MessageBody> _body;
    readonly DictionarySendHeaders _headers;

    IMessageSerializer _serializer = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public MessageSendContext(TMessage message, CancellationToken cancellationToken = default)
        : base(cancellationToken)
    {
        Message = message;

        _headers = new DictionarySendHeaders();

        var messageId = NewId.Next();

        MessageId = messageId.ToGuid();
        SentTime = messageId.Timestamp;

        SupportedMessageTypes = MessageTypeCache<TMessage>.MessageTypeNames.ToArray();

        _body = new Lazy<MessageBody>(() => GetMessageBody());
    }

    /// <summary>Set to true if the message is being published.</summary>
    public bool IsPublish { get; set; }

    /// <summary>Gets the body.</summary>
    public MessageBody Body => _body.Value;

    /// <summary>Gets or sets the delay.</summary>
    public virtual TimeSpan? Delay { get; set; }

    /// <summary>Gets or sets the message id.</summary>
    public Guid? MessageId { get; set; }
    /// <summary>Gets or sets the request id.</summary>
    public Guid? RequestId { get; set; }
    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId { get; set; }

    /// <summary>Gets or sets the conversation id.</summary>
    public Guid? ConversationId { get; set; }
    /// <summary>Gets or sets the initiator id.</summary>
    public Guid? InitiatorId { get; set; }

    /// <summary>Gets or sets the scheduled message id.</summary>
    public Guid? ScheduledMessageId { get; set; }

    /// <summary>Gets the headers.</summary>
    public SendHeaders Headers => _headers;

    /// <summary>Gets or sets the source address.</summary>
    public Uri? SourceAddress { get; set; }
    /// <summary>Gets or sets the destination address.</summary>
    public Uri? DestinationAddress { get; set; }
    /// <summary>Gets or sets the response address.</summary>
    public Uri? ResponseAddress { get; set; }
    /// <summary>Gets or sets the fault address.</summary>
    public Uri? FaultAddress { get; set; }

    /// <summary>Gets or sets the time to live.</summary>
    public TimeSpan? TimeToLive { get; set; }
    /// <summary>Gets or sets the sent time.</summary>
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

    /// <summary>Gets or sets the content type.</summary>
    public ContentType? ContentType { get; set; }

    /// <summary>Gets or sets the serializer.</summary>
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

    /// <summary>Gets or sets the serialization.</summary>
    public ISerialization Serialization { get; set; } = null!;
    /// <summary>Gets or sets the supported message types.</summary>
    public string[] SupportedMessageTypes { get; set; }

    /// <summary>Gets the body length.</summary>
    public long? BodyLength => _body.IsValueCreated ? _body.Value.Length : default;

    /// <summary>Creates proxy.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>The created proxy.</returns>
    public SendContext<T> CreateProxy<T>(T message)
        where T : class
    {
        return new SendContextProxy<T>(this, message);
    }

    /// <summary>Gets or sets the durable.</summary>
    public bool Durable { get; set; } = true;

    /// <summary>Gets the message.</summary>
    public TMessage Message { get; }

    /// <summary>Gets or sets the mandatory.</summary>
    public bool Mandatory { get; set; }

    /// <summary>Writes properties to.</summary>
    /// <param name="properties">The properties.</param>
    public virtual void WritePropertiesTo(IDictionary<string, object> properties)
    {
        if (!Durable)
            properties[PropertyNames.Durable] = false;
        if (Mandatory)
            properties[PropertyNames.Mandatory] = true;
        if (Delay.HasValue)
            properties[PropertyNames.Delay] = Delay.Value;
    }

    /// <summary>Reads properties from.</summary>
    /// <param name="properties">The properties.</param>
    public virtual void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        Durable = ReadBoolean(properties, PropertyNames.Durable, true);
        Mandatory = ReadBoolean(properties, PropertyNames.Mandatory);
        Delay = ReadTimeSpan(properties, PropertyNames.Delay);
    }

    MessageBody GetMessageBody()
    {
        return Serializer?.GetMessageBody(this) ?? throw new SerializationException("Unable to serialize message, no serializer specified.");
    }

    /// <summary>Reads string.</summary>
    /// <param name="properties">The properties.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>Reads string array.</summary>
    /// <param name="properties">The properties.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <returns>The string array produced by the operation.</returns>
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

    /// <summary>Reads time span.</summary>
    /// <param name="properties">The properties.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The time span produced by the operation.</returns>
    protected static TimeSpan? ReadTimeSpan(IReadOnlyDictionary<string, object> properties, string key, TimeSpan? defaultValue = null)
    {
        var value = ReadString(properties, key);

        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return _timeSpanConverter.TryConvert(value, out var result) ? result : defaultValue;
    }

    /// <summary>Reads enum.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="properties">The properties.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The t produced by the operation.</returns>
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

    /// <summary>Reads byte.</summary>
    /// <param name="properties">The properties.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The byte produced by the operation.</returns>
    protected static byte ReadByte(IReadOnlyDictionary<string, object> properties, string key, byte defaultValue = default)
    {
        if (!properties.TryGetValue(key, out var value))
            return defaultValue;

        if (value is byte byteValue)
            return byteValue;

        var longValue = ReadLong(properties, key);

        return longValue.HasValue ? (byte)longValue.Value : defaultValue;
    }

    /// <summary>Reads int.</summary>
    /// <param name="properties">The properties.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The int produced by the operation.</returns>
    protected static int? ReadInt(IReadOnlyDictionary<string, object> properties, string key, int? defaultValue = null)
    {
        var longValue = ReadLong(properties, key);

        return longValue.HasValue ? (int)longValue.Value : defaultValue;
    }

    /// <summary>Reads short.</summary>
    /// <param name="properties">The properties.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The short produced by the operation.</returns>
    protected static short? ReadShort(IReadOnlyDictionary<string, object> properties, string key, short? defaultValue = null)
    {
        var longValue = ReadLong(properties, key);

        return longValue.HasValue ? (short)longValue.Value : defaultValue;
    }

    /// <summary>Reads long.</summary>
    /// <param name="properties">The properties.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The long produced by the operation.</returns>
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

    /// <summary>Reads boolean.</summary>
    /// <param name="properties">The properties.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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
