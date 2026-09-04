namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Looks for a property that can be used as a CorrelationId message header, and
/// applies a filter to set it on message send if available
/// </summary>
public class CorrelationIdSendTopologyConvention :
    ISendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public CorrelationIdSendTopologyConvention()
    {
        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(typeof(CorrelationIdMessageSendTopologyConvention<>), new Factory());
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
            return new CorrelationIdMessageSendTopologyConvention<T>();
        }
    }
}
