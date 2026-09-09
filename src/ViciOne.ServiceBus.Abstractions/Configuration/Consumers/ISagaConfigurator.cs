using System;
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a saga and its message-specific middleware pipelines.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaConfigurator<TSaga> :
    IPipeConfigurator<SagaConsumeContext<TSaga>>,
    ISagaConfigurationObserverConnector,
    IConsumeConfigurator,
    IOptionsSet
    where TSaga : class
{
    /// <summary>Sets the maximum number of messages this saga may process concurrently.</summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>Configures middleware invoked before the saga repository.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configure">The callback to configure the message pipeline.</param>
    void Message<T>(Action<ISagaMessageConfigurator<T>> configure)
        where T : class;

    /// <summary>
    /// Configures message-specific middleware invoked after the saga repository.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configure">The callback to configure the message pipeline.</param>
    void SagaMessage<T>(Action<ISagaMessageConfigurator<TSaga, T>> configure)
        where T : class;
}
