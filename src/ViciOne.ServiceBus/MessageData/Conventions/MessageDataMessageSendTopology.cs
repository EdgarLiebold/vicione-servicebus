using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.MessageData.Conventions;

public class MessageDataMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly TransformFilter<T> _transformFilter;

    public MessageDataMessageSendTopology(IMessageInitializer<T> initializer)
    {
        _transformFilter = new TransformFilter<T>(initializer);
    }

    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        if (builder.IsImplemented)
            return;

        builder.AddFilter(_transformFilter);
    }
}
