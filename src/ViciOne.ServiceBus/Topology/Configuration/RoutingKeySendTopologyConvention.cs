namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a routing key send topology convention implementation.
/// </summary>
public class RoutingKeySendTopologyConvention :
    IRoutingKeySendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingKeySendTopologyConvention()
    {
        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(typeof(IRoutingKeyMessageSendTopologyConvention<>), new Factory());
    }

    /// <summary>
    /// Attempts to get message send topology convention.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="convention">The convention value.</param>
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
