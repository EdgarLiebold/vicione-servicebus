using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Defines the topology for message data message consume.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class MessageDataMessageConsumeTopology<T> :
    IMessageConsumeTopology<T>
    where T : class
{
    readonly TransformFilter<T> _transformFilter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="initializer">The initializer.</param>
    public MessageDataMessageConsumeTopology(IMessageInitializer<T> initializer)
    {
        _transformFilter = new TransformFilter<T>(initializer);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(ITopologyPipeBuilder<ConsumeContext<T>> builder)
    {
        builder.AddFilter(_transformFilter);
    }
}
