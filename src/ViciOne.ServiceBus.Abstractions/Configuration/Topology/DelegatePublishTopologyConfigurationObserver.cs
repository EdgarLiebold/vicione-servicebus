using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds matching topologies from another publish topology as delegates.</summary>
public sealed class DelegatePublishTopologyConfigurationObserver :
    IPublishTopologyConfigurationObserver
{
    readonly IPublishTopologyConfigurator _publishTopology;

    /// <summary>Initializes the observer with the publish topology that supplies delegates.</summary>
    /// <param name="publishTopology">The publish topology configurator to delegate to.</param>
    public DelegatePublishTopologyConfigurationObserver(IPublishTopologyConfigurator publishTopology)
    {
        _publishTopology = publishTopology ?? throw new ArgumentNullException(nameof(publishTopology));
    }

    /// <summary>Adds the matching publish-message topology to the created configurator.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The publish-message topology configurator receiving the delegate.</param>
    public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        IMessagePublishTopologyConfigurator<T> publishTopologyConfigurator = _publishTopology.GetMessageTopology<T>();

        configurator.AddDelegate(publishTopologyConfigurator);
    }
}
