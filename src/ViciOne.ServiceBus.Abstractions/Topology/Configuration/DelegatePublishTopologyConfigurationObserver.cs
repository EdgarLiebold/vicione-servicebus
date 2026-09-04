namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a delegate publish topology configuration observer implementation.
/// </summary>
public class DelegatePublishTopologyConfigurationObserver :
    IPublishTopologyConfigurationObserver
{
    readonly IPublishTopologyConfigurator _publishTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    public DelegatePublishTopologyConfigurationObserver(IPublishTopologyConfigurator publishTopology)
    {
        _publishTopology = publishTopology;
    }

    /// <summary>
    /// Performs the message topology created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class
    {
        IMessagePublishTopologyConfigurator<T> publishTopologyConfigurator = _publishTopology.GetMessageTopology<T>();

        configurator.AddDelegate(publishTopologyConfigurator);
    }
}
