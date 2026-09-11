using System.Net.Mime;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Copies the body of the receive context to the send context unmodified.</summary>
public sealed class CopyBodySerializer :
    IMessageSerializer
{
    readonly MessageBody _body;
    readonly string _contentType;

    /// <summary>Creates a serializer that reuses an encoded body without transforming it.</summary>
    /// <param name="contentType">The media type of the encoded body.</param>
    /// <param name="body">The encoded body to reuse.</param>
    public CopyBodySerializer(ContentType contentType, MessageBody body)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        _body = body ?? throw new ArgumentNullException(nameof(body));

        _contentType = contentType.ToString();
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
        return _body;
    }
}
