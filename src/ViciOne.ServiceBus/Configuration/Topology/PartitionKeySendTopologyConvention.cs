namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides a cached partition-key convention for each message contract.</summary>
public sealed class PartitionKeySendTopologyConvention :
    IPartitionKeySendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>Initializes an empty per-message convention cache.</summary>
    public PartitionKeySendTopologyConvention()
    {
        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(new Factory());
    }

    bool IMessageSendTopologyConvention.TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
    {
        return _cache.GetOrAdd<T, IMessageSendTopologyConvention<T>>().TryGetMessageSendTopologyConvention(out convention);
    }


    sealed class Factory :
        IConventionTypeFactory<IMessageSendTopologyConvention>
    {
        IMessageSendTopologyConvention IConventionTypeFactory<IMessageSendTopologyConvention>.Create<T>()
        {
            return new PartitionKeyMessageSendTopologyConvention<T>();
        }
    }
}
