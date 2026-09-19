using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Owns the final envelope emitted by an external bounded serializer after bus-owned admission.</summary>
internal sealed class BoundedSerializerMessageBody : MessageBody, IPayloadAdmittedMessageBody
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    private readonly PayloadAdmissionSerializationContext _admission;
    private readonly byte[] _envelope;
    private readonly SerializedTransportTextFormat _textFormat;

    private BoundedSerializerMessageBody(
        byte[] envelope,
        PayloadAdmissionSerializationContext admission,
        SerializedTransportTextFormat textFormat)
    {
        _envelope = envelope;
        _admission = admission;
        _textFormat = textFormat;
    }

    public long Length => _envelope.LongLength;

    PayloadAdmissionSerializationContext IPayloadAdmittedMessageBody.AdmissionContext => _admission;

    public byte[] ToArray() => (byte[])_envelope.Clone();

    public Stream OpenReadStream() => new MemoryStream(_envelope, writable: false);

    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = _textFormat switch
        {
            SerializedTransportTextFormat.Utf8 => StrictUtf8.GetString(_envelope),
            SerializedTransportTextFormat.Base64 => Convert.ToBase64String(_envelope),
            _ => null,
        };
        return text is not null;
    }

    internal static BoundedSerializerMessageBody Create<T>(
        SendContext<T> context,
        IBoundedMessageSerializer serializer,
        PayloadAdmissionSerializationContext admission)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(admission);

        SerializedTransportTextFormat textFormat = serializer.TransportTextFormat;
        if (!Enum.IsDefined(textFormat))
            throw new ArgumentOutOfRangeException(nameof(serializer), textFormat, "Unknown transport text format.");

        IPayloadSerializationBuffer bodyBuffer = admission.Runtime.CreateSerializedBodyBuffer();
        serializer.WriteSerializedBody(context, bodyBuffer);

        // The serializer may retain memory supplied by IBufferWriter. Admission and the
        // second stage therefore consume a private snapshot, never its writable buffer.
        byte[] body = bodyBuffer.WrittenMemory.ToArray();
        _ = admission.Runtime.EvaluateSerializedBody(body, admission.MessageDataOffloadObserved);

        IPayloadSerializationBuffer envelopeBuffer = admission.Runtime.CreateTransportEnvelopeBuffer();
        using (var bodyStream = new MemoryStream(body, writable: false))
            serializer.WriteTransportEnvelope(context, bodyStream, envelopeBuffer);

        // A private copy also prevents a retained envelope writer from changing emitted
        // bytes after the admission decision or durable-proof hash was recorded.
        byte[] envelope = envelopeBuffer.WrittenMemory.ToArray();
        if (!serializer.TryLocateSerializedBody(envelope, out int offset, out int length)
            || offset < 0
            || length != body.Length
            || offset > envelope.Length - length
            || !envelope.AsSpan(offset, length).SequenceEqual(body))
        {
            throw new InvalidOperationException(
                "The bounded serializer did not embed the exact admitted application body as one contiguous range in its final envelope.");
        }

        if (textFormat == SerializedTransportTextFormat.Utf8)
            _ = StrictUtf8.GetCharCount(envelope);
        admission.Runtime.ValidateTransportEnvelope(envelope);

        return new BoundedSerializerMessageBody(envelope, admission, textFormat);
    }
}
