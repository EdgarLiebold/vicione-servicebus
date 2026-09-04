namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Defines the contract for broker topology.
/// </summary>
public interface BrokerTopology :
    IProbeSite
{
    /// <summary>
    /// Gets the exchanges value.
    /// </summary>
    Exchange[] Exchanges { get; }
    /// <summary>
    /// Gets the queues value.
    /// </summary>
    Queue[] Queues { get; }
    /// <summary>
    /// Gets the exchange bindings value.
    /// </summary>
    ExchangeToExchangeBinding[] ExchangeBindings { get; }
    /// <summary>
    /// Gets the queue bindings value.
    /// </summary>
    ExchangeToQueueBinding[] QueueBindings { get; }
}
