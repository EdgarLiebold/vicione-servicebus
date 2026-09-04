using ViciOne.ServiceBus.AzureServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus send topology.
/// </summary>
public interface IServiceBusSendTopology :
    ISendTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IServiceBusMessageSendTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    SendSettings GetSendSettings(ServiceBusEndpointAddress address);

    /// <summary>
    /// Gets error settings.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    SendSettings GetErrorSettings(IServiceBusQueueConfigurator configurator);
    /// <summary>
    /// Gets dead letter settings.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    SendSettings GetDeadLetterSettings(IServiceBusQueueConfigurator configurator);
}
