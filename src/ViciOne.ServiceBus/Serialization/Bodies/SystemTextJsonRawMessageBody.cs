using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Serializes an outgoing message directly as UTF-8 JSON without a ServiceBus envelope.</summary>
/// <typeparam name="TMessage">The message contract serialized by the body.</typeparam>
internal sealed class SystemTextJsonRawMessageBody<TMessage> :
    MessageBody
    where TMessage : class
{
    readonly byte[] _content;

    /// <summary>Creates an owned snapshot of the raw JSON body.</summary>
    /// <param name="context">The outgoing message and admission policy.</param>
    /// <param name="options">The JSON serializer options used while creating the snapshot.</param>
    /// <param name="message">An alternate message value, or <see langword="null" /> to use the context message.</param>
    public SystemTextJsonRawMessageBody(SendContext<TMessage> context, JsonSerializerOptions options, object? message = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _content = Serialize(context, options, message ?? context.Message);
    }

    /// <summary>Gets the exact encoded UTF-8 byte length.</summary>
    public long Length => _content.LongLength;

    /// <summary>Copies the raw JSON content into a new array.</summary>
    /// <returns>An independently mutable copy of the raw JSON content.</returns>
    public byte[] ToArray() => (byte[])_content.Clone();

    /// <summary>Opens a non-writable stream over the raw JSON body.</summary>
    /// <returns>A readable stream positioned at the start of the body.</returns>
    public Stream OpenReadStream() => new MemoryStream(_content, false);

    /// <summary>Tries to get the raw JSON text.</summary>
    /// <param name="text">The serialized message text.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = MessageDefaults.Encoding.GetString(_content);
        return true;
    }

    static byte[] Serialize(SendContext<TMessage> context, JsonSerializerOptions options, object? message)
    {
        try
        {
            if (!context.TryGetPayload(out PayloadAdmissionSerializationContext? admission))
                return JsonSerializer.SerializeToUtf8Bytes(message, options);

            IPayloadSerializationBuffer bodyBuffer = admission.Runtime.CreateSerializedBodyBuffer();
            using (var writer = new Utf8JsonWriter(bodyBuffer))
                JsonSerializer.Serialize(writer, message, message?.GetType() ?? typeof(object), options);

            _ = admission.Runtime.EvaluateSerializedBody(bodyBuffer.WrittenMemory, admission.MessageDataOffloadObserved);

            // A raw application body is also the transport envelope, so the same bytes must satisfy both limits.
            IPayloadSerializationBuffer envelopeBuffer = admission.Runtime.CreateTransportEnvelopeBuffer();
            bodyBuffer.WrittenMemory.Span.CopyTo(envelopeBuffer.GetSpan(bodyBuffer.WrittenCount));
            envelopeBuffer.Advance(bodyBuffer.WrittenCount);
            admission.Runtime.ValidateTransportEnvelope(envelopeBuffer.WrittenMemory);
            return envelopeBuffer.WrittenMemory.ToArray();
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
}
