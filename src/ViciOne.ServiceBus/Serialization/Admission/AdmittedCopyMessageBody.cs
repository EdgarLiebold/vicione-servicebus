using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Owns the exact bounded bytes that copy/forwarding transports will later emit.</summary>
internal sealed class AdmittedCopyMessageBody : MessageBody, IPayloadAdmittedMessageBody
{
    static readonly Encoding _strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    readonly PayloadAdmissionSerializationContext _admissionContext;
    readonly byte[] _content;
    readonly TransportTextKind _textKind;

    AdmittedCopyMessageBody(byte[] content, PayloadAdmissionSerializationContext admissionContext, TransportTextKind textKind)
    {
        _content = content;
        _admissionContext = admissionContext;
        _textKind = textKind;
    }

    public long Length => _content.LongLength;

    PayloadAdmissionSerializationContext IPayloadAdmittedMessageBody.AdmissionContext => _admissionContext;

    public byte[] ToArray() => (byte[])_content.Clone();

    public Stream OpenReadStream() => new MemoryStream(_content, writable: false);

    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = _textKind switch
        {
            TransportTextKind.Json => _strictUtf8.GetString(_content),
            TransportTextKind.Base64 => Convert.ToBase64String(_content),
            _ => null,
        };
        return text is not null;
    }

    internal static AdmittedCopyMessageBody Create(
        MessageBody source,
        ContentType contentType,
        ISerialization serialization,
        PayloadAdmissionSerializationContext admission,
        DurablePayloadAdmissionProof? durableProof,
        string? persistedContentType = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        byte[] content = Snapshot(source, admission.Runtime.MaximumTransportEnvelopeBytes);
        return CreateOwned(content, contentType, serialization, admission, durableProof, persistedContentType);
    }

    internal static AdmittedCopyMessageBody Create(
        ReadOnlyMemory<byte> source,
        ContentType contentType,
        ISerialization serialization,
        PayloadAdmissionSerializationContext admission,
        DurablePayloadAdmissionProof? durableProof,
        string? persistedContentType = null)
    {
        ArgumentNullException.ThrowIfNull(admission);
        if (source.Length > admission.Runtime.MaximumTransportEnvelopeBytes)
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.TransportEnvelope,
                source.Length,
                admission.Runtime.MaximumTransportEnvelopeBytes,
                $"Final serialized transport envelope is {source.Length} bytes, exceeding the configured maximum of {admission.Runtime.MaximumTransportEnvelopeBytes} bytes.");

        return CreateOwned(source.ToArray(), contentType, serialization, admission, durableProof, persistedContentType);
    }

    internal static AdmittedCopyMessageBody CreateOwned(
        byte[] content,
        ContentType contentType,
        ISerialization serialization,
        PayloadAdmissionSerializationContext admission,
        DurablePayloadAdmissionProof? durableProof,
        string? persistedContentType = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(serialization);
        ArgumentNullException.ThrowIfNull(admission);

        // A send-only content type need not have a registered deserializer. Without an
        // exact extractor, the entire copied envelope is conservatively charged as body.
        serialization.TryGetMessageDeserializer(contentType, out IMessageDeserializer? deserializer);

        TransportTextKind textKind = GetTransportTextKind(deserializer, contentType);

        if (content.Length > admission.Runtime.MaximumTransportEnvelopeBytes)
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.TransportEnvelope,
                content.Length,
                admission.Runtime.MaximumTransportEnvelopeBytes,
                $"Final serialized transport envelope is {content.Length} bytes, exceeding the configured maximum of {admission.Runtime.MaximumTransportEnvelopeBytes} bytes.");

        if (textKind == TransportTextKind.Json && !IsValidJson(content))
            textKind = TransportTextKind.None;
        if (durableProof is { } proof)
            admission.AdmitDurableReplay(content, persistedContentType ?? contentType.ToString(), proof);
        else
        {
            ReadOnlyMemory<byte> body = deserializer switch
            {
                SystemTextJsonRawMessageSerializer => content,
                SystemTextJsonMessageSerializer jsonSerializer =>
                    JsonEnvelopeMessageValue.Extract(content, jsonSerializer.AdmissionOptions),
                ICopiedEnvelopeBodyExtractor extractor => extractor.ExtractSerializedBody(content),
                ICopiedEnvelopeBodyLocator locator => LocateSerializedBody(locator, content),
                _ => content,
            };

            _ = admission.Runtime.EvaluateSerializedBody(body, admission.MessageDataOffloadObserved);
            admission.Runtime.ValidateTransportEnvelope(content);
        }

        return new AdmittedCopyMessageBody(content, admission, textKind);
    }

    static TransportTextKind GetTransportTextKind(IMessageDeserializer? deserializer, ContentType contentType)
        => deserializer switch
        {
            SystemTextJsonMessageSerializer or SystemTextJsonRawMessageSerializer => TransportTextKind.Json,
            ICopiedEnvelopeBodyExtractor when contentType.MediaType.Equals(
                "application/vnd.vicione.servicebus+msgpack", StringComparison.OrdinalIgnoreCase) => TransportTextKind.Base64,
            _ when contentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                || contentType.MediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase) => TransportTextKind.Json,
            _ => TransportTextKind.None,
        };

    static ReadOnlyMemory<byte> LocateSerializedBody(ICopiedEnvelopeBodyLocator locator, byte[] content)
    {
        if (!locator.TryLocateSerializedBody(content, out int offset, out int length)
            || offset < 0
            || offset > content.Length
            || length < 0
            || length > content.Length - offset)
        {
            throw new InvalidOperationException(
                "The copied envelope body locator did not provide a valid serialized application-body range.");
        }

        return content.AsMemory(offset, length);
    }

    static byte[] Snapshot(MessageBody source, int maximumEnvelopeBytes)
    {
        long declaredLength = source.Length;
        if (declaredLength < 0)
            throw new InvalidOperationException("The copied message body reported a negative byte length.");
        if (declaredLength > maximumEnvelopeBytes)
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.TransportEnvelope,
                declaredLength,
                maximumEnvelopeBytes,
                $"Final serialized transport envelope is {declaredLength} bytes, exceeding the configured maximum of {maximumEnvelopeBytes} bytes.");

        // The one retained buffer becomes the body emitted by transports. No separate
        // validation buffer or untrusted ToArray result can be changed after admission.
        var content = new byte[(int)declaredLength];
        using Stream stream = source.OpenReadStream();
        int read = 0;
        while (read < content.Length)
        {
            int count = stream.Read(content, read, content.Length - read);
            if (count == 0)
                throw new InvalidOperationException("The copied message body ended before its declared byte length.");
            read += count;
        }

        if (stream.ReadByte() != -1)
            throw new InvalidOperationException("The copied message body exceeded its declared byte length.");

        return content;
    }

    static bool IsValidJson(byte[] content)
    {
        try
        {
            _ = _strictUtf8.GetCharCount(content);

            // JSONB requires strict JSON, even when a message deserializer permits
            // comments or trailing commas. Depth is bounded by the admitted byte
            // length, not the reader's unrelated default of 64 levels.
            var reader = new Utf8JsonReader(content, new JsonReaderOptions
            {
                MaxDepth = Math.Max(1, content.Length),
            });
            if (!reader.Read())
                return false;

            while (reader.Read())
            {
            }

            return reader.BytesConsumed == content.Length;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    enum TransportTextKind { None, Json, Base64 }
}
