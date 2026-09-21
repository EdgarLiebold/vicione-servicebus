using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Provides an immutable snapshot of RabbitMQ exchange, queue, and binding declarations.</summary>
public class RabbitMqBrokerTopology :
    BrokerTopology
{
    readonly ExchangeToExchangeBinding[] _exchangeBindings;
    readonly Exchange[] _exchanges;
    readonly ExchangeToQueueBinding[] _queueBindings;
    readonly Queue[] _queues;

    /// <summary>Creates a broker-topology snapshot from the supplied declarations.</summary>
    /// <param name="exchanges">The exchange declarations.</param>
    /// <param name="exchangeBindings">The exchange-to-exchange bindings.</param>
    /// <param name="queues">The queue declarations.</param>
    /// <param name="queueBindings">The exchange-to-queue bindings.</param>
    public RabbitMqBrokerTopology(IEnumerable<Exchange> exchanges, IEnumerable<ExchangeToExchangeBinding> exchangeBindings, IEnumerable<Queue> queues,
        IEnumerable<ExchangeToQueueBinding> queueBindings)
    {
        _exchanges = exchanges.ToArray();
        _queues = queues.ToArray();
        _exchangeBindings = exchangeBindings.ToArray();
        _queueBindings = queueBindings.ToArray();
    }

    /// <summary>Gets the exchange declarations.</summary>
    public Exchange[] Exchanges => (Exchange[])_exchanges.Clone();
    /// <summary>Gets the queue declarations.</summary>
    public Queue[] Queues => (Queue[])_queues.Clone();
    /// <summary>Gets the exchange-to-exchange bindings.</summary>
    public ExchangeToExchangeBinding[] ExchangeBindings => (ExchangeToExchangeBinding[])_exchangeBindings.Clone();
    /// <summary>Gets the exchange-to-queue bindings.</summary>
    public ExchangeToQueueBinding[] QueueBindings => (ExchangeToQueueBinding[])_queueBindings.Clone();

    void IProbeSite.Probe(ProbeContext context)
    {
        foreach (var exchange in _exchanges)
        {
            var exchangeScope = context.CreateScope("exchange");
            exchangeScope.Set(new
            {
                Name = exchange.ExchangeName,
                Type = exchange.ExchangeType,
                exchange.Durable,
                exchange.AutoDelete
            });
            foreach (KeyValuePair<string, object?> argument in exchange.ExchangeArguments)
            {
                var argumentScope = exchangeScope.CreateScope("argument");
                argumentScope.Add("key", argument.Key);
                if (argument.Value != null)
                    argumentScope.Add("value", argument.Value);
            }
        }

        foreach (var queue in _queues)
        {
            var exchangeScope = context.CreateScope("queue");
            exchangeScope.Set(new
            {
                Name = queue.QueueName,
                queue.Durable,
                queue.AutoDelete,
                queue.Exclusive
            });
            foreach (KeyValuePair<string, object?> argument in queue.QueueArguments)
            {
                var argumentScope = exchangeScope.CreateScope("argument");
                argumentScope.Add("key", argument.Key);
                if (argument.Value != null)
                    argumentScope.Add("value", argument.Value);
            }
        }

        foreach (var binding in _exchangeBindings)
        {
            var exchangeScope = context.CreateScope("exchange-binding");
            exchangeScope.Set(new
            {
                Source = binding.Source.ExchangeName,
                Destination = binding.Destination.ExchangeName,
                binding.RoutingKey
            });
            foreach (KeyValuePair<string, object?> argument in binding.Arguments)
            {
                var argumentScope = exchangeScope.CreateScope("argument");
                argumentScope.Add("key", argument.Key);
                if (argument.Value != null)
                    argumentScope.Add("value", argument.Value);
            }
        }

        foreach (var binding in _queueBindings)
        {
            var exchangeScope = context.CreateScope("queue-binding");
            exchangeScope.Set(new
            {
                Source = binding.Source.ExchangeName,
                Destination = binding.Destination.QueueName,
                binding.RoutingKey
            });
            foreach (KeyValuePair<string, object?> argument in binding.Arguments)
            {
                var argumentScope = exchangeScope.CreateScope("argument");
                argumentScope.Add("key", argument.Key);
                if (argument.Value != null)
                    argumentScope.Add("value", argument.Value);
            }
        }
    }
}
