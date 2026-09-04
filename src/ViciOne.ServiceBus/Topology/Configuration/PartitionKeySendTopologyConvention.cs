namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a partition key send topology convention implementation.
/// </summary>
public class PartitionKeySendTopologyConvention :
    IPartitionKeySendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PartitionKeySendTopologyConvention()
    {
        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(typeof(IPartitionKeyMessageSendTopologyConvention<>), new Factory());
    }

    bool IMessageSendTopologyConvention.TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
    {
        return _cache.GetOrAdd<T, IMessageSendTopologyConvention<T>>().TryGetMessageSendTopologyConvention(out convention);
    }


    class Factory :
        IConventionTypeFactory<IMessageSendTopologyConvention>
    {
        IMessageSendTopologyConvention IConventionTypeFactory<IMessageSendTopologyConvention>.Create<T>()
        {
            return new PartitionKeyMessageSendTopologyConvention<T>(null);
        }
    }
}
