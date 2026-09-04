using System.Net.Mime;

namespace ViciOne.ServiceBus.Configuration;

public interface ISetSerializerMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    void SetSerializer(ContentType contentType);
}
