using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a session id send topology convention implementation.
/// </summary>
public class SessionIdSendTopologyConvention :
    ISessionIdSendTopologyConvention
{
    readonly ITopologyConventionCache<IMessageSendTopologyConvention> _cache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public SessionIdSendTopologyConvention()
    {
        DefaultFormatter = new EmptySessionIdFormatter();

        _cache = new TopologyConventionCache<IMessageSendTopologyConvention>(typeof(ISessionIdMessageSendTopologyConvention<>), new Factory());
    }

    bool IMessageSendTopologyConvention.TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
    {
        return _cache.GetOrAdd<T, IMessageSendTopologyConvention<T>>().TryGetMessageSendTopologyConvention(out convention);
    }

    /// <summary>
    /// Gets or sets the default formatter value.
    /// </summary>
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
