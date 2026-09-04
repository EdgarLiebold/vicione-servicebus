using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus send topology configurator.
/// </summary>
public interface IServiceBusSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IServiceBusSendTopology
{
    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    Action<IServiceBusEntityConfigurator> ConfigureErrorSettings { set; }
    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
    Action<IServiceBusEntityConfigurator> ConfigureDeadLetterSettings { set; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IServiceBusMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;
}
