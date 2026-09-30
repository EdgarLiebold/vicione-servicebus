using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Serializes an outgoing message and its ServiceBus envelope as UTF-8 JSON.</summary>
/// <typeparam name="TMessage">The message contract contained by the envelope.</typeparam>
internal sealed class SystemTextJsonMessageBody<TMessage> :
    MessageBody, IPayloadAdmittedMessageBody
    where TMessage : class
{
    readonly byte[] _content;
    readonly PayloadAdmissionSerializationContext? _admissionContext;

    /// <summary>Creates an owned snapshot of the encoded envelope.</summary>
    /// <param name="context">The outgoing message and metadata.</param>
    /// <param name="options">The JSON serializer options used while creating the snapshot.</param>
    /// <param name="envelope">An existing envelope to encode, or <see langword="null" /> to create one from the context.</param>
    public SystemTextJsonMessageBody(SendContext<TMessage> context, JsonSerializerOptions options, MessageEnvelope? envelope = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        context.TryGetPayload(out PayloadAdmissionSerializationContext? admission);
        _admissionContext = admission;
        _content = Serialize(context, options, envelope);
    }

    /// <summary>Gets the exact encoded UTF-8 byte length.</summary>
    public long Length => _content.LongLength;

    PayloadAdmissionSerializationContext? IPayloadAdmittedMessageBody.AdmissionContext => _admissionContext;

    /// <summary>Copies the encoded envelope into a new array.</summary>
    /// <returns>An independently mutable copy of the encoded envelope.</returns>
    public byte[] ToArray() => (byte[])_content.Clone();

    /// <summary>Opens a non-writable stream over the encoded envelope.</summary>
    /// <returns>A readable stream positioned at the start of the envelope.</returns>
    public Stream OpenReadStream() => new MemoryStream(_content, false);

    /// <summary>Tries to get the encoded envelope as JSON text.</summary>
    /// <param name="text">The serialized envelope text.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = MessageDefaults.Encoding.GetString(_content);
        return true;
    }

    static byte[] Serialize(SendContext<TMessage> context, JsonSerializerOptions options, MessageEnvelope? existingEnvelope)
    {
        try
        {
            var envelope = existingEnvelope ?? new JsonMessageEnvelope(context, context.Message);

            if (!context.TryGetPayload(out PayloadAdmissionSerializationContext? admission))
            {
                BindHeaders(context, envelope, options);
                return JsonSerializer.SerializeToUtf8Bytes(envelope, options);
            }

            var writerOptions = new JsonWriterOptions
            {
                Indented = options.WriteIndented,
                IndentCharacter = options.IndentCharacter,
                IndentSize = options.IndentSize,
                NewLine = options.NewLine,
                Encoder = options.Encoder,
            };

            IPayloadSerializationBuffer bodyBuffer = admission.Runtime.CreateSerializedBodyBuffer();
            using (var bodyWriter = new Utf8JsonWriter(bodyBuffer, writerOptions))
            {
                object? message = envelope.Message;
                JsonSerializer.Serialize(bodyWriter, message, message?.GetType() ?? typeof(object), options);
            }

            using JsonDocument bodyDocument = JsonDocument.Parse(bodyBuffer.WrittenMemory,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = options.AllowTrailingCommas,
                    CommentHandling = options.ReadCommentHandling == JsonCommentHandling.Allow
                        ? JsonCommentHandling.Skip
                        : options.ReadCommentHandling,
                    MaxDepth = options.MaxDepth,
                });
            var boundedEnvelope = new JsonMessageEnvelope(envelope)
            {
                Message = bodyDocument.RootElement,
            };
            BindHeaders(context, boundedEnvelope, options);

            IPayloadSerializationBuffer envelopeBuffer = admission.Runtime.CreateTransportEnvelopeBuffer();
            using (var envelopeWriter = new Utf8JsonWriter(envelopeBuffer, writerOptions))
                JsonSerializer.Serialize(envelopeWriter, boundedEnvelope, options);

            ReadOnlyMemory<byte> finalBody = JsonEnvelopeMessageValue.Extract(envelopeBuffer.WrittenMemory, options);
            _ = admission.Runtime.EvaluateSerializedBody(finalBody, admission.MessageDataOffloadObserved);
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

    private static void BindHeaders(
        SendContext<TMessage> context, MessageEnvelope envelope, JsonSerializerOptions options)
    {
        if (context is MessageSendContext<TMessage> sendContext && envelope.Headers is { } headers)
            sendContext.BindSerializedEnvelopeHeaders(headers,
                (value, type) => JsonSerializer.SerializeToUtf8Bytes(value, type, options),
                (type, encoded) => JsonSerializer.Deserialize(encoded, type, options));

    }
}
