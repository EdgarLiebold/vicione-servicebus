using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

public class SystemTextJsonMessageBody<TMessage> :
    MessageBody
    where TMessage : class
{
    readonly SendContext<TMessage> _context;
    readonly JsonSerializerOptions _options;
    byte[]? _bytes;
    MessageEnvelope? _envelope;
    string? _string;

    public SystemTextJsonMessageBody(SendContext<TMessage> context, JsonSerializerOptions options, MessageEnvelope? envelope = null)
    {
        _context = context;
        _options = options;
        _envelope = envelope;
    }

    /// <summary>
    /// The number of bytes this body transmits, which is by definition the length of what
    /// <see cref="GetBytes" /> returns, whichever accessor ran first. Answering from whichever
    /// representation happened to exist reported a character count after a string read and nothing
    /// at all before the first read, so the same body gave three different answers.
    /// </summary>
    public long? Length => GetBytes().LongLength;

    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

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

    public string GetString()
    {
        if (_string != null)
            return _string;

        _string = Encoding.UTF8.GetString(GetBytes());
        return _string;
    }
}
