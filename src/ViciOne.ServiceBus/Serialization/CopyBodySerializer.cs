using System.Net.Mime;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Copies the body of the receive context to the send context unmodified
/// </summary>
public class CopyBodySerializer :
    IMessageSerializer
{
    readonly MessageBody _body;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    /// <param name="body">The body value.</param>
    public CopyBodySerializer(ContentType contentType, MessageBody body)
    {
        _body = body;

        ContentType = contentType;
    }

    /// <summary>
    /// Gets the content type value.
    /// </summary>
    public ContentType ContentType { get; }

    /// <summary>
    /// Gets message body.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        return _body;
    }
}
