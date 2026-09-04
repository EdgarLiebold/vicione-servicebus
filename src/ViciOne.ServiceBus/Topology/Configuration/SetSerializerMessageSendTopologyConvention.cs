using System.Net.Mime;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

public class SetSerializerMessageSendTopologyConvention<TMessage> :
    ISetSerializerMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    ContentType? _contentType;

    bool IMessageSendTopologyConvention.TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
    {
        convention = this as IMessageSendTopologyConvention<T>;

        return convention != null;
    }

    bool IMessageSendTopologyConvention<TMessage>.TryGetMessageSendTopology(
        [NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
    {
        if (_contentType != null)
        {
            messageSendTopology = new SetSerializerMessageSendTopology<TMessage>(_contentType);
            return true;
        }

        messageSendTopology = null;
        return false;
    }

    public void SetSerializer(ContentType contentType)
    {
        _contentType = contentType;
    }
}
