using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Adds a message-data storage transform to a send topology.</summary>
/// <typeparam name="T">The outgoing message contract type.</typeparam>
internal sealed class MessageDataMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly TransformFilter<T> _transformFilter;

    /// <summary>Creates a topology around the discovered object-graph initializer.</summary>
    /// <param name="initializer">The initializer that applies message-data storage policy.</param>
    public MessageDataMessageSendTopology(IMessageInitializer<T> initializer)
    {
        _transformFilter = new TransformFilter<T>(initializer ?? throw new ArgumentNullException(nameof(initializer)));
    }

    /// <summary>Adds the storage transform only when the builder is not marked as implemented.</summary>
    /// <param name="builder">The send-topology pipe builder.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.IsImplemented)
            return;

        builder.AddFilter(_transformFilter);
    }
}
