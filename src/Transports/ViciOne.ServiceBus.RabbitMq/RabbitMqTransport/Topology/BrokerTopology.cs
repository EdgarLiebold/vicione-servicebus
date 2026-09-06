namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Describes the exchanges, queues, and bindings that must exist on a RabbitMQ broker.</summary>
public interface BrokerTopology :
    IProbeSite
{
    /// <summary>Gets the exchange declarations.</summary>
    Exchange[] Exchanges { get; }
    /// <summary>Gets the queue declarations.</summary>
    Queue[] Queues { get; }
    /// <summary>Gets the exchange-to-exchange bindings.</summary>
    ExchangeToExchangeBinding[] ExchangeBindings { get; }
    /// <summary>Gets the exchange-to-queue bindings.</summary>
    ExchangeToQueueBinding[] QueueBindings { get; }
}
