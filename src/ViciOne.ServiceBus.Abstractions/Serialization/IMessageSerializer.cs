using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>A message serializer is responsible for serializing a message. Shocking, I know.</summary>
public interface IMessageSerializer
{
    /// <summary>Gets the content type.</summary>
    ContentType ContentType { get; }

    /// <summary>
    /// Returns a message body, for the serializer, which can be used by the transport to obtain the
    /// serialized message in the desired format.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The message body.</returns>
    MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class;
}
