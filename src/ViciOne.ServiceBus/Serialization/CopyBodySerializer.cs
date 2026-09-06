using System.Net.Mime;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Copies the body of the receive context to the send context unmodified.</summary>
public class CopyBodySerializer :
    IMessageSerializer
{
    readonly MessageBody _body;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    /// <param name="body">The body.</param>
    public CopyBodySerializer(ContentType contentType, MessageBody body)
    {
        _body = body;

        ContentType = contentType;
    }

    /// <summary>Gets the content type.</summary>
    public ContentType ContentType { get; }

    /// <summary>Gets message body.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The message body.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        return _body;
    }
}
