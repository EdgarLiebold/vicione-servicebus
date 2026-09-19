using System.Net.Mime;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Copies the body of the receive context to the send context unmodified.</summary>
public sealed class CopyBodySerializer :
    IMessageSerializer
{
    readonly MessageBody? _body;
    readonly string _contentType;
    readonly DurablePayloadAdmissionProof? _durableProof;
    readonly ReadOnlyMemory<byte>? _durableBytes;

    /// <summary>Creates a serializer that reuses an encoded body without transforming it.</summary>
    /// <param name="contentType">The media type of the encoded body.</param>
    /// <param name="body">The encoded body to reuse.</param>
    public CopyBodySerializer(ContentType contentType, MessageBody body)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        _body = body ?? throw new ArgumentNullException(nameof(body));

        _contentType = contentType.ToString();
    }

    internal CopyBodySerializer(ContentType contentType, MessageBody body, DurablePayloadAdmissionProof? durableProof)
        : this(contentType, body)
    {
        _durableProof = durableProof;
    }

    internal CopyBodySerializer(string persistedContentType, ReadOnlyMemory<byte> body, DurablePayloadAdmissionProof? durableProof)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(persistedContentType);
        _contentType = persistedContentType;
        _durableBytes = body;
        _durableProof = durableProof;
    }

    /// <summary>Gets the media type of the copied body.</summary>
    public ContentType ContentType => new(_contentType);

    /// <summary>Returns the unchanged encoded body.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The outgoing message context.</param>
    /// <returns>The body supplied to this serializer.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.TryGetPayload(out PayloadAdmissionSerializationContext? admission))
        {
            return _durableBytes is { } durableBytes
                ? AdmittedCopyMessageBody.Create(durableBytes, new ContentType(_contentType), context.Serialization, admission, _durableProof, _contentType)
                : AdmittedCopyMessageBody.Create(_body!, new ContentType(_contentType), context.Serialization, admission, _durableProof, _contentType);
        }

        return _durableBytes is { } bytes ? new BinaryMessageBody(bytes) : _body!;
    }
}
