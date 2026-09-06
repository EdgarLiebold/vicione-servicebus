namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies conventions for routing key send topology.</summary>
public class RoutingKeySendTopologyConvention :
    IRoutingKeySendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>Initializes a new instance.</summary>
    public RoutingKeySendTopologyConvention()
    {
        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(typeof(IRoutingKeyMessageSendTopologyConvention<>), new Factory());
    }

    /// <summary>Attempts to get message send topology convention.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="convention">Receives the convention produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class
    {
        return _cache.GetOrAdd<T, IMessageSendTopologyConvention<T>>().TryGetMessageSendTopologyConvention(out convention);
    }


    class Factory :
        IConventionTypeFactory<IMessageSendTopologyConvention>
    {
        IMessageSendTopologyConvention IConventionTypeFactory<IMessageSendTopologyConvention>.Create<T>()
        {
            return new RoutingKeyMessageSendTopologyConvention<T>(null);
        }
    }
}
