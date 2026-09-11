using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Adds a message-data loading transform to a consume topology.</summary>
/// <typeparam name="T">The consumed message contract type.</typeparam>
internal sealed class MessageDataMessageConsumeTopology<T> :
    IMessageConsumeTopology<T>
    where T : class
{
    readonly TransformFilter<T> _transformFilter;

    /// <summary>Creates a topology around the discovered object-graph initializer.</summary>
    /// <param name="initializer">The initializer that resolves message-data properties.</param>
    public MessageDataMessageConsumeTopology(IMessageInitializer<T> initializer)
    {
        _transformFilter = new TransformFilter<T>(initializer ?? throw new ArgumentNullException(nameof(initializer)));
    }

    /// <summary>Adds the loading transform to the consume pipe.</summary>
    /// <param name="builder">The consume-topology pipe builder.</param>
    public void Apply(ITopologyPipeBuilder<ConsumeContext<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(_transformFilter);
    }
}
