using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Owns the mutable transport send state for one typed message.</summary>
/// <typeparam name="TMessage">The outgoing message contract.</typeparam>
public class MessageSendContext<TMessage> :
    BasePipeContext,
    TransportSendContext<TMessage>,
    IForwardedMessageTypeContext
    where TMessage : class
{
    static readonly TimeSpanTypeConverter _timeSpanConverter = new TimeSpanTypeConverter();

    readonly Lazy<MessageBody> _body;
    readonly DictionarySendHeaders _headers;

    IMessageSerializer? _serializer;
    ISerialization? _serialization;
    TransportBodyMaterializer.MetadataSnapshot? _serializedMetadata;
    object? _serializedNativeMetadata;
    string[]? _serializedMessageTypes;
    SerializedEnvelopeHeaderSnapshot? _serializedEnvelopeHeaders;
    bool _messageTypesBound;
    string[]? _authorizedForwardedMessageTypes;
    bool _forwardedTypesAuthorized;
    bool _bodyCreationInProgress;
    MessageException? _metadataFailure;

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
    public MessageBody Body
    {
        get
        {
            if (_metadataFailure is { } rejected)
                throw rejected;

            ThrowIfEnvelopeHeadersChanged();

            TransportBodyMaterializer.MetadataSnapshot beforeBody =
                _serializedMetadata ?? TransportBodyMaterializer.MetadataSnapshot.Capture(this);
            ITransportSendMetadata? nativeContext = this as ITransportSendMetadata;
            object? nativeBeforeBody = _serializedNativeMetadata ?? nativeContext?.CaptureNativeMetadata();
            if (beforeBody.ChangedField(this) is { } staleField)
            {
                beforeBody.Restore(this);
                _serializedMetadata ??= beforeBody;
                throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(staleField);
            }
            if (nativeBeforeBody is not null && nativeContext?.ChangedNativeField(nativeBeforeBody) is { } staleNativeField)
            {
                nativeContext.RestoreNativeMetadata(nativeBeforeBody);
                throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(staleNativeField);
            }
            if (MessageTypesChanged)
            {
                RestoreSerializedMessageTypes();
                throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(nameof(SupportedMessageTypes));
            }

            string[]? typesBeforeGetter = _messageTypesBound ? null : SupportedMessageTypes?.ToArray();
            try
            {
                _bodyCreationInProgress = true;
                _forwardedTypesAuthorized = false;
                MessageBody body = _body.Value;
                if (!_messageTypesBound)
                {
                    if ((_forwardedTypesAuthorized && !SameMessageTypes(_authorizedForwardedMessageTypes, SupportedMessageTypes)) ||
                        (!_forwardedTypesAuthorized && !SameMessageTypes(typesBeforeGetter, SupportedMessageTypes)))
                    {
                        SupportedMessageTypes = typesBeforeGetter!;
                        throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(nameof(SupportedMessageTypes));
                    }
                    _serializedMessageTypes = SupportedMessageTypes?.ToArray();
                    _messageTypesBound = true;
                }
                if (beforeBody.ChangedField(this) is { } changedField)
                {
                    beforeBody.Restore(this);
                    _serializedMetadata ??= beforeBody;
                    throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(changedField);
                }
                if (nativeBeforeBody is not null && nativeContext?.ChangedNativeField(nativeBeforeBody) is { } changedNativeField)
                {
                    nativeContext.RestoreNativeMetadata(nativeBeforeBody);
                    throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(changedNativeField);
                }
                if (MessageTypesChanged)
                {
                    RestoreSerializedMessageTypes();
                    throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(nameof(SupportedMessageTypes));
                }
                ThrowIfEnvelopeHeadersChanged();

                _serializedMetadata ??= beforeBody;
                _serializedNativeMetadata ??= nativeBeforeBody;
                return body;
            }
            catch (Exception failure)
            {
                RestoreEnvelopeHeadersAfterFailure(failure);
                if (beforeBody.ChangedField(this) is not null)
                {
                    beforeBody.Restore(this);
                    TransportBodyMaterializer.MarkMutationFailure(failure);
                }
                if (nativeBeforeBody is not null && nativeContext?.ChangedNativeField(nativeBeforeBody) is not null)
                {
                    nativeContext.RestoreNativeMetadata(nativeBeforeBody);
                    TransportBodyMaterializer.MarkMutationFailure(failure);
                }
                if (MessageTypesChanged)
                {
                    RestoreSerializedMessageTypes();
                    TransportBodyMaterializer.MarkMutationFailure(failure);
                }
                else if (!_messageTypesBound && !SameMessageTypes(typesBeforeGetter, SupportedMessageTypes))
                {
                    SupportedMessageTypes = typesBeforeGetter!;
                    TransportBodyMaterializer.MarkMutationFailure(failure);
                }

                throw;
            }
            finally
            {
                _bodyCreationInProgress = false;
            }
        }
    }

    internal void BindSerializedEnvelopeHeaders(
        IReadOnlyDictionary<string, object?> projectedHeaders,
        Func<object?, Type, byte[]> encode,
        Func<Type, byte[], object?> decode)
    {
        if (_bodyCreationInProgress)
            _serializedEnvelopeHeaders ??= new SerializedEnvelopeHeaderSnapshot(
                projectedHeaders, _headers, encode, decode);
    }

    internal SerializedEnvelopeHeaderSnapshot? SerializedEnvelopeHeaders => _serializedEnvelopeHeaders;

    internal void ThrowIfEnvelopeHeadersChanged()
    {
        if (_serializedEnvelopeHeaders?.ChangedHeader(_headers) is not { } changedHeader)
            return;

        MessageException failure = TransportBodyMaterializer.CreateMutationFailure<TMessage>($"Headers[{changedHeader}]");
        _metadataFailure = failure;
        try
        {
            _serializedEnvelopeHeaders.Restore(_headers);
        }
        catch (Exception restoreFailure)
        {
            failure.Data["HeaderRestoreFailure"] = restoreFailure;
        }
        throw failure;
    }

    internal void RestoreEnvelopeHeadersAfterFailure(Exception failure)
    {
        if (_serializedEnvelopeHeaders?.ChangedHeader(_headers) is null)
            return;

        TransportBodyMaterializer.MarkMutationFailure(failure);
        try
        {
            _serializedEnvelopeHeaders.Restore(_headers);
        }
        catch (Exception restoreFailure)
        {
            failure.Data["HeaderRestoreFailure"] = restoreFailure;
        }
    }

    void IForwardedMessageTypeContext.AcceptForwardedMessageTypes(string[]? messageTypes)
    {
        if (_bodyCreationInProgress)
        {
            _authorizedForwardedMessageTypes = messageTypes?.ToArray();
            _forwardedTypesAuthorized = true;
        }
    }

    internal TransportBodyMaterializer.MetadataSnapshot? SerializedMetadata => _serializedMetadata;

    internal object? SerializedNativeMetadata => _serializedNativeMetadata;

    internal void AcceptProviderNativeMetadataUpdate(Action update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (_metadataFailure is { } rejected)
            throw rejected;
        if (this is not ITransportSendMetadata nativeContext)
            throw new InvalidOperationException("The send context has no provider-native metadata.");
        if (_serializedNativeMetadata is { } bound && nativeContext.ChangedNativeField(bound) is { } changedField)
        {
            nativeContext.RestoreNativeMetadata(bound);
            throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(changedField);
        }

        update();
        if (_serializedNativeMetadata is not null)
            _serializedNativeMetadata = nativeContext.CaptureNativeMetadata();
    }

    internal bool MessageTypesChanged => _messageTypesBound && !SameMessageTypes(_serializedMessageTypes, SupportedMessageTypes);

    static bool SameMessageTypes(string[]? expected, string[]? actual) =>
        expected is null ? actual is null : actual is not null && expected.SequenceEqual(actual, StringComparer.Ordinal);

    internal void RestoreSerializedMessageTypes() => SupportedMessageTypes = _serializedMessageTypes?.ToArray()!;

    /// <summary>Accepts the scheduling identifier returned by a broker after successful delivery.</summary>
    /// <param name="scheduledMessageId">The broker-confirmed scheduling identifier.</param>
    internal void AcceptBrokerScheduledMessageId(Guid scheduledMessageId)
    {
        if (_metadataFailure is { } rejected)
            throw rejected;
        if (_serializedMetadata is { } serialized && serialized.ChangedField(this) is { } changedField)
        {
            serialized.Restore(this);
            throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(changedField);
        }
        if (MessageTypesChanged)
        {
            RestoreSerializedMessageTypes();
            throw _metadataFailure = TransportBodyMaterializer.CreateMutationFailure<TMessage>(nameof(SupportedMessageTypes));
        }

        ScheduledMessageId = scheduledMessageId;
        if (_serializedMetadata is { } bound)
            _serializedMetadata = bound with { ScheduledMessageId = scheduledMessageId };
    }

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

    /// <summary>Assigns stable durable-admission identities and clears broker dispatch time before serialization.</summary>
    /// <param name="idempotencyKey">The identity shared by equivalent durable admission attempts.</param>
    /// <param name="correlationId">The optional application correlation identity.</param>
    internal void SetDurableAdmissionMetadata(Guid idempotencyKey, Guid? correlationId)
    {
        if (_body.IsValueCreated)
            throw new InvalidOperationException("Durable admission metadata must be fixed before serialization.");

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
        get => _serializer ?? throw new InvalidOperationException("A message serializer has not been configured.");
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (_body.IsValueCreated)
                throw new InvalidOperationException("The message was already serialized.");

            _serializer = value;
            ContentType = value.ContentType;
        }
    }

    /// <inheritdoc />
    public ISerialization Serialization
    {
        get => _serialization ?? throw new InvalidOperationException("Serialization has not been configured.");
        set => _serialization = value ?? throw new ArgumentNullException(nameof(value));
    }
    /// <inheritdoc />
    public string[] SupportedMessageTypes { get; set; }

    /// <inheritdoc />
    public long? BodyLength => _body.IsValueCreated ? _body.Value.Length : null;

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
        IMessageSerializer serializer = _serializer
            ?? throw new SerializationException("Unable to serialize the message because no serializer is configured.");

        if (!this.TryGetPayload(out PayloadAdmissionSerializationContext? admission))
            return serializer.GetMessageBody(this);

        if (serializer is IBoundedMessageSerializer boundedSerializer)
            return BoundedSerializerMessageBody.Create(this, boundedSerializer, admission);

        // Built-in serializers still own their established admitted bodies. A legacy external
        // serializer cannot obtain the internal operation marker and fails at the physical boundary.
        MessageBody body = serializer.GetMessageBody(this);
        if (body is not IPayloadAdmittedMessageBody)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Serialization",
                    "unknown",
                    $"Serializer '{serializer.GetType().FullName}' does not support bounded payload admission.",
                    $"Implement {nameof(IBoundedMessageSerializer)} for this serializer"));
        }

        return body;
    }

    /// <summary>Reads a UTF-8 or native string value from a transport property bag.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or has an unsupported representation.</param>
    /// <returns>The decoded string, or <paramref name="defaultValue" /> when no supported value is available.</returns>
    protected static string? ReadString(IReadOnlyDictionary<string, object> properties, string key, string? defaultValue = null)
    {
        ValidatePropertyLookup(properties, key);

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
        ValidatePropertyLookup(properties, key);

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
        ValidatePropertyLookup(properties, key);

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
        where T : struct, Enum
    {
        ValidatePropertyLookup(properties, key);

        var value = ReadString(properties, key);
        return value is not null && Enum.TryParse(value, true, out T enumValue) ? enumValue : defaultValue;
    }

    /// <summary>Reads a byte-compatible integer from a transport property bag.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or invalid.</param>
    /// <returns>The byte value, or <paramref name="defaultValue" /> when no compatible value is available.</returns>
    /// <exception cref="OverflowException">The stored integer cannot be represented as a byte.</exception>
    protected static byte ReadByte(IReadOnlyDictionary<string, object> properties, string key, byte defaultValue = default)
    {
        ValidatePropertyLookup(properties, key);

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
        ValidatePropertyLookup(properties, key);

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
        ValidatePropertyLookup(properties, key);

        var longValue = ReadLong(properties, key);

        return longValue.HasValue ? checked((short)longValue.Value) : defaultValue;
    }

    /// <summary>Reads a 64-bit integer from a native integral, character, invariant string, or UTF-8 property value.</summary>
    /// <param name="properties">The transport property bag.</param>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or invalid.</param>
    /// <returns>The integer value, or <paramref name="defaultValue" /> when no compatible value is available.</returns>
    /// <exception cref="OverflowException">The stored unsigned integer cannot be represented as a 64-bit signed integer.</exception>
    protected static long? ReadLong(IReadOnlyDictionary<string, object> properties, string key, long? defaultValue = null)
    {
        ValidatePropertyLookup(properties, key);

        if (properties.TryGetValue(key, out var value))
        {
            if (value is long longValue)
                return longValue;

            switch (value)
            {
                case sbyte signedByteValue:
                    return signedByteValue;
                case byte byteValue:
                    return byteValue;
                case int intValue:
                    return intValue;
                case short shortValue:
                    return shortValue;
                case ushort unsignedShortValue:
                    return unsignedShortValue;
                case uint unsignedIntValue:
                    return unsignedIntValue;
                case ulong unsignedLongValue:
                    return checked((long)unsignedLongValue);
                case char charValue:
                    return charValue;
                case nint nativeIntValue:
                    return nativeIntValue;
                case nuint nativeUnsignedIntValue:
                    return checked((long)nativeUnsignedIntValue);
            }

            if (value is string text)
                return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue) ? longValue : defaultValue;

            if (value is byte[] bytes)
            {
                text = Encoding.UTF8.GetString(bytes);
                return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue) ? longValue : defaultValue;
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
        ValidatePropertyLookup(properties, key);

        if (!properties.TryGetValue(key, out var value))
            return defaultValue;

        if (value is bool boolValue)
            return boolValue;

        var longValue = ReadLong(properties, key);

        return longValue.HasValue ? longValue.Value != 0 : defaultValue;
    }

    static void ValidatePropertyLookup(IReadOnlyDictionary<string, object> properties, string key)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }


    static class PropertyNames
    {
        public const string Delay = "Delay";
        public const string Durable = "Durable";
        public const string Mandatory = "Mandatory";
    }
}
