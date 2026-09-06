using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql consume topology configurator.
/// </summary>
public interface ISqlConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    ISqlConsumeTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new ISqlMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    void AddSpecification(ISqlConsumeTopologySpecification specification);

    /// <summary>
    /// Bind an exchange, using the configurator
    /// </summary>
    /// <param name="topicName"></param>
    /// <param name="configure"></param>
    void Subscribe(string topicName, Action<ISqlTopicSubscriptionConfigurator>? configure = null);
}
