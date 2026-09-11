using System.Net.Mime;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.Tests.Serialization;

internal sealed class RejectingMessageSerializer(string failureMessage) : IMessageSerializer
{
    public ContentType ContentType { get; } = new("application/vnd.vicione.rejected-message");

    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class =>
        throw new SerializationException(failureMessage);
}
