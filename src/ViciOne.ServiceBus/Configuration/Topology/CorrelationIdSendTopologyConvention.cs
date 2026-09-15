namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a correlation convention for each message contract and caches it for reuse.
/// </summary>
public sealed class CorrelationIdSendTopologyConvention :
    ISendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>Initializes an empty per-message convention cache.</summary>
    public CorrelationIdSendTopologyConvention()
    {
        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(new Factory());
    }

    /// <summary>Gets the correlation convention for a message contract.</summary>
    /// <typeparam name="T">The sent message contract type.</typeparam>
    /// <param name="convention">Receives the cached message-specific convention.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class
    {
        return _cache.GetOrAdd<T, IMessageSendTopologyConvention<T>>().TryGetMessageSendTopologyConvention(out convention);
    }


    sealed class Factory :
        IConventionTypeFactory<IMessageSendTopologyConvention>
    {
        IMessageSendTopologyConvention IConventionTypeFactory<IMessageSendTopologyConvention>.Create<T>()
        {
            return new CorrelationIdMessageSendTopologyConvention<T>();
        }
    }
}
