using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq broker topology implementation.
/// </summary>
public class RabbitMqBrokerTopology :
    BrokerTopology
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchanges">The exchanges value.</param>
    /// <param name="exchangeBindings">The exchange bindings value.</param>
    /// <param name="queues">The queues value.</param>
    /// <param name="queueBindings">The queue bindings value.</param>
    public RabbitMqBrokerTopology(IEnumerable<Exchange> exchanges, IEnumerable<ExchangeToExchangeBinding> exchangeBindings, IEnumerable<Queue> queues,
        IEnumerable<ExchangeToQueueBinding> queueBindings)
    {
        Exchanges = exchanges.ToArray();
        Queues = queues.ToArray();
        ExchangeBindings = exchangeBindings.ToArray();
        QueueBindings = queueBindings.ToArray();
    }

    /// <summary>
    /// Gets the exchanges value.
    /// </summary>
    public Exchange[] Exchanges { get; }
    /// <summary>
    /// Gets the queues value.
    /// </summary>
    public Queue[] Queues { get; }
    /// <summary>
    /// Gets the exchange bindings value.
    /// </summary>
    public ExchangeToExchangeBinding[] ExchangeBindings { get; }
    /// <summary>
    /// Gets the queue bindings value.
    /// </summary>
    public ExchangeToQueueBinding[] QueueBindings { get; }

    void IProbeSite.Probe(ProbeContext context)
    {
        foreach (var exchange in Exchanges)
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

        foreach (var queue in Queues)
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

        foreach (var binding in ExchangeBindings)
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

        foreach (var binding in QueueBindings)
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
