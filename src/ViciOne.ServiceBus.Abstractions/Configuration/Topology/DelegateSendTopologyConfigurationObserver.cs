using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds matching topologies from another send topology as delegates.</summary>
public sealed class DelegateSendTopologyConfigurationObserver :
    ISendTopologyConfigurationObserver
{
    readonly ISendTopology _sendTopology;

    /// <summary>Initializes the observer with the send topology that supplies delegates.</summary>
    /// <param name="sendTopology">The send topology to delegate to.</param>
    public DelegateSendTopologyConfigurationObserver(ISendTopology sendTopology)
    {
        _sendTopology = sendTopology ?? throw new ArgumentNullException(nameof(sendTopology));
    }

    /// <summary>Adds the matching send-message topology to the created configurator.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configuration">The send-message topology configurator receiving the delegate.</param>
    public void MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> configuration)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IMessageSendTopologyConfigurator<T> specification = _sendTopology.GetMessageTopology<T>();

        configuration.AddDelegate(specification);
    }
}
