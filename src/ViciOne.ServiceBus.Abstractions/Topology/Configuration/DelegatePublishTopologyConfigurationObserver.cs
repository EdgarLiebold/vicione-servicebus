namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes delegate publish topology configuration events.</summary>
public class DelegatePublishTopologyConfigurationObserver :
    IPublishTopologyConfigurationObserver
{
    readonly IPublishTopologyConfigurator _publishTopology;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="publishTopology">The publish topology.</param>
    public DelegatePublishTopologyConfigurationObserver(IPublishTopologyConfigurator publishTopology)
    {
        _publishTopology = publishTopology;
    }

    /// <summary>Reports that message topology has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class
    {
        IMessagePublishTopologyConfigurator<T> publishTopologyConfigurator = _publishTopology.GetMessageTopology<T>();

        configurator.AddDelegate(publishTopologyConfigurator);
    }
}
