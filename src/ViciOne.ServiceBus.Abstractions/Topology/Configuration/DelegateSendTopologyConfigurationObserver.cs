namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes delegate send topology configuration events.</summary>
public class DelegateSendTopologyConfigurationObserver :
    ISendTopologyConfigurationObserver
{
    readonly ISendTopology _sendTopology;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sendTopology">The send topology.</param>
    public DelegateSendTopologyConfigurationObserver(ISendTopology sendTopology)
    {
        _sendTopology = sendTopology;
    }

    /// <summary>Reports that message topology has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configuration">The callback used to configure the component.</param>
    public void MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> configuration)
        where T : class
    {
        IMessageSendTopologyConfigurator<T> specification = _sendTopology.GetMessageTopology<T>();

        configuration.AddDelegate(specification);
    }
}
