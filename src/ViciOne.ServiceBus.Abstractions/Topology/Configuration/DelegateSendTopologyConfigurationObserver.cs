namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a delegate send topology configuration observer implementation.
/// </summary>
public class DelegateSendTopologyConfigurationObserver :
    ISendTopologyConfigurationObserver
{
    readonly ISendTopology _sendTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sendTopology">The send topology value.</param>
    public DelegateSendTopologyConfigurationObserver(ISendTopology sendTopology)
    {
        _sendTopology = sendTopology;
    }

    /// <summary>
    /// Performs the message topology created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configuration">The configuration callback.</param>
    public void MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> configuration)
        where T : class
    {
        IMessageSendTopologyConfigurator<T> specification = _sendTopology.GetMessageTopology<T>();

        configuration.AddDelegate(specification);
    }
}
