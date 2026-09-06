namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies conventions for partition key send topology.</summary>
public class PartitionKeySendTopologyConvention :
    IPartitionKeySendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>Initializes a new instance.</summary>
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
