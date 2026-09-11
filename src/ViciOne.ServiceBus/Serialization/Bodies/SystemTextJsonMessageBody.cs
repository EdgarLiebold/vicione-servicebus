using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Serializes an outgoing message and its ServiceBus envelope as UTF-8 JSON.</summary>
/// <typeparam name="TMessage">The message contract contained by the envelope.</typeparam>
internal sealed class SystemTextJsonMessageBody<TMessage> :
    MessageBody
    where TMessage : class
{
    readonly SendContext<TMessage> _context;
    readonly JsonSerializerOptions _options;
    byte[]? _bytes;
    MessageEnvelope? _envelope;
    string? _string;

    /// <summary>Creates a lazily encoded envelope body.</summary>
    /// <param name="context">The outgoing message and metadata.</param>
    /// <param name="options">The immutable JSON serializer options.</param>
    /// <param name="envelope">An existing envelope to encode, or <see langword="null" /> to create one from the context.</param>
    public SystemTextJsonMessageBody(SendContext<TMessage> context, JsonSerializerOptions options, MessageEnvelope? envelope = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _envelope = envelope;
    }

    /// <summary>Gets the exact encoded UTF-8 byte length.</summary>
    public long? Length => GetBytes().LongLength;

    /// <summary>Opens a non-writable stream over the encoded envelope.</summary>
    /// <returns>A readable stream positioned at the start of the envelope.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>Gets the encoded UTF-8 envelope.</summary>
    /// <returns>The serialized envelope bytes.</returns>
    public byte[] GetBytes()
    {
        if (_bytes != null)
            return _bytes;

        if (_string != null)
        {
            _bytes = Encoding.UTF8.GetBytes(_string);
            return _bytes;
        }

        try
        {
            var envelope = _envelope ??= new JsonMessageEnvelope(_context, _context.Message);

            if (!_context.TryGetPayload(out PayloadAdmissionSerializationContext? admission))
            {
                _bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, _options);
                return _bytes;
            }

            IPayloadSerializationBuffer bodyBuffer = admission.Runtime.CreateSerializedBodyBuffer();
            using (var bodyWriter = new Utf8JsonWriter(bodyBuffer))
            {
                object? message = envelope.Message;
                JsonSerializer.Serialize(bodyWriter, message, message?.GetType() ?? typeof(object), _options);
            }

            _ = admission.Runtime.EvaluateSerializedBody(bodyBuffer.WrittenMemory, admission.MessageDataOffloadObserved);

            using JsonDocument bodyDocument = JsonDocument.Parse(bodyBuffer.WrittenMemory);
            var boundedEnvelope = new JsonMessageEnvelope(envelope)
            {
                Message = bodyDocument.RootElement,
            };

            IPayloadSerializationBuffer envelopeBuffer = admission.Runtime.CreateTransportEnvelopeBuffer();
            using (var envelopeWriter = new Utf8JsonWriter(envelopeBuffer))
                JsonSerializer.Serialize(envelopeWriter, boundedEnvelope, _options);

            admission.Runtime.ValidateTransportEnvelope(envelopeBuffer.WrittenMemory);
            _bytes = envelopeBuffer.WrittenMemory.ToArray();

            return _bytes;
        }
        catch (PayloadAdmissionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SerializationException("Failed to serialize message", ex);
        }
    }

    /// <summary>Gets the encoded envelope as JSON text.</summary>
    /// <returns>The serialized envelope text.</returns>
    public string GetString()
    {
        if (_string != null)
            return _string;

        _string = Encoding.UTF8.GetString(GetBytes());
        return _string;
    }
}
