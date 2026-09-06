using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures saga.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaConfigurator<TSaga> :
    IPipeConfigurator<SagaConsumeContext<TSaga>>,
    ISagaConfigurationObserverConnector,
    IConsumeConfigurator,
    IOptionsSet
    where TSaga : class
{
    /// <summary>Gets or sets the concurrent message limit.</summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>Add middleware to the message pipeline, which is invoked prior to the saga repository.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configure">The callback to configure the message pipeline.</param>
    void Message<T>(Action<ISagaMessageConfigurator<T>> configure)
        where T : class;

    /// <summary>
    /// Add middleware to the saga pipeline, for the specified message type, which is invoked
    /// after the saga repository.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configure">The callback to configure the message pipeline.</param>
    void SagaMessage<T>(Action<ISagaMessageConfigurator<TSaga, T>> configure)
        where T : class;
}
