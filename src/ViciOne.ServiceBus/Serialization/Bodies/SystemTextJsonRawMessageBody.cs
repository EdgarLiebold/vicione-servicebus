using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Serializes an outgoing message directly as UTF-8 JSON without a ServiceBus envelope.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
internal sealed class SystemTextJsonRawMessageBody<TMessage> :
    MessageBody
    where TMessage : class
{
    readonly SendContext<TMessage> _context;
    readonly object? _message;
    readonly JsonSerializerOptions _options;
    byte[]? _bytes;
    string? _string;

    /// <summary>Creates a lazily encoded raw JSON body.</summary>
    /// <param name="context">The outgoing message and admission policy.</param>
    /// <param name="options">The immutable JSON serializer options.</param>
    /// <param name="message">An alternate message value, or <see langword="null" /> to use the context message.</param>
    public SystemTextJsonRawMessageBody(SendContext<TMessage> context, JsonSerializerOptions options, object? message = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _message = message ?? context.Message;
    }

    /// <summary>Gets the exact encoded UTF-8 byte length.</summary>
    public long? Length => GetBytes().LongLength;

    /// <summary>Opens a non-writable stream over the raw JSON body.</summary>
    /// <returns>A readable stream positioned at the start of the body.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>Gets the raw JSON body as UTF-8 bytes.</summary>
    /// <returns>The serialized message bytes.</returns>
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
            if (!_context.TryGetPayload(out PayloadAdmissionSerializationContext? admission))
            {
                _bytes = JsonSerializer.SerializeToUtf8Bytes(_message, _options);
                return _bytes;
            }

            IPayloadSerializationBuffer bodyBuffer = admission.Runtime.CreateSerializedBodyBuffer();
            using (var writer = new Utf8JsonWriter(bodyBuffer))
                JsonSerializer.Serialize(writer, _message, _message?.GetType() ?? typeof(object), _options);

            _ = admission.Runtime.EvaluateSerializedBody(bodyBuffer.WrittenMemory, admission.MessageDataOffloadObserved);

            // A raw application body is also the transport envelope, so the same bytes must satisfy both limits.
            IPayloadSerializationBuffer envelopeBuffer = admission.Runtime.CreateTransportEnvelopeBuffer();
            bodyBuffer.WrittenMemory.Span.CopyTo(envelopeBuffer.GetSpan(bodyBuffer.WrittenCount));
            envelopeBuffer.Advance(bodyBuffer.WrittenCount);
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

    /// <summary>Gets the raw JSON body as text.</summary>
    /// <returns>The serialized message text.</returns>
    public string GetString()
    {
        if (_string != null)
            return _string;

        _string = Encoding.UTF8.GetString(GetBytes());
        return _string;
    }
}
