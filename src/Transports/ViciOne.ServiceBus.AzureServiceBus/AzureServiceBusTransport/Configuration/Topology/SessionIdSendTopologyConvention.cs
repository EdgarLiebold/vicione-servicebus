using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Caches per-message conventions that assign Azure Service Bus session identifiers.</summary>
public class SessionIdSendTopologyConvention :
    ISessionIdSendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>Initializes the convention cache with an empty default formatter.</summary>
    public SessionIdSendTopologyConvention()
    {
        DefaultFormatter = new EmptySessionIdFormatter();

        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(typeof(ISessionIdMessageSendTopologyConvention<>), new Factory());
    }

    bool IMessageSendTopologyConvention.TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
    {
        return _cache.GetOrAdd<T, IMessageSendTopologyConvention<T>>().TryGetMessageSendTopologyConvention(out convention);
    }

    /// <summary>Gets or sets the formatter inherited by newly configured message conventions.</summary>
    public ISessionIdFormatter DefaultFormatter { get; set; }


    class Factory :
        IConventionTypeFactory<IMessageSendTopologyConvention>
    {
        IMessageSendTopologyConvention IConventionTypeFactory<IMessageSendTopologyConvention>.Create<T>()
        {
            return new SessionIdMessageSendTopologyConvention<T>(null);
        }
    }
}
