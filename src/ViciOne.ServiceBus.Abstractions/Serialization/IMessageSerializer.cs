using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Serializes typed send contexts into transport message bodies.</summary>
public interface IMessageSerializer
{
    /// <summary>Gets the media content type produced by this serializer.</summary>
    ContentType ContentType { get; }

    /// <summary>Creates the serialized message body used for transport dispatch.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="context">The send context containing the message and envelope metadata.</param>
    /// <returns>The lazily or eagerly serialized transport body.</returns>
    MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class;
}
