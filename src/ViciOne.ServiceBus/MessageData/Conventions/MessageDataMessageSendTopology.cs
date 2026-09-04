using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>
/// Provides a message data message send topology implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class MessageDataMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly TransformFilter<T> _transformFilter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="initializer">The initializer value.</param>
    public MessageDataMessageSendTopology(IMessageInitializer<T> initializer)
    {
        _transformFilter = new TransformFilter<T>(initializer);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        if (builder.IsImplemented)
            return;

        builder.AddFilter(_transformFilter);
    }
}
