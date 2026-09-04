using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a system text json raw message body implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SystemTextJsonRawMessageBody<TMessage> :
    MessageBody
    where TMessage : class
{
    readonly SendContext<TMessage> _context;
    readonly object? _message;
    readonly JsonSerializerOptions _options;
    byte[]? _bytes;
    string? _string;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="options">The options value.</param>
    /// <param name="message">The message value.</param>
    public SystemTextJsonRawMessageBody(SendContext<TMessage> context, JsonSerializerOptions options, object? message = null)
    {
        _context = context;
        _options = options;
        _message = message ?? context.Message;
    }

    /// <summary>
    /// The number of bytes this body transmits, which is by definition the length of what
    /// <see cref="GetBytes" /> returns, whichever accessor ran first. Answering from whichever
    /// representation happened to exist reported a character count after a string read and nothing
    /// at all before the first read, so the same body gave three different answers.
    /// </summary>
    public long? Length => GetBytes().LongLength;

    /// <summary>
    /// Gets stream.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>
    /// Gets bytes.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

            // Raw JSON has no wrapper object: the application body is also the final transport
            // envelope. Copying the already bounded bytes into the independently bounded envelope
            // owner preserves single-pass application serialization while enforcing both limits.
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

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public string GetString()
    {
        if (_string != null)
            return _string;

        _string = Encoding.UTF8.GetString(GetBytes());
        return _string;
    }
}
