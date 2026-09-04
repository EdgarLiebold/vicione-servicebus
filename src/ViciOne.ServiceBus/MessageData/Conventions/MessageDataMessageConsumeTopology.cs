using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.MessageData.Conventions;

public class MessageDataMessageConsumeTopology<T> :
    IMessageConsumeTopology<T>
    where T : class
{
    readonly TransformFilter<T> _transformFilter;

    public MessageDataMessageConsumeTopology(IMessageInitializer<T> initializer)
    {
        _transformFilter = new TransformFilter<T>(initializer);
    }

    public void Apply(ITopologyPipeBuilder<ConsumeContext<T>> builder)
    {
        builder.AddFilter(_transformFilter);
    }
}
