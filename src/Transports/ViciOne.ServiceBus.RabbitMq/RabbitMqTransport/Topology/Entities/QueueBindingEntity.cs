using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Represents a de-duplicated exchange-to-queue binding in a broker topology.</summary>
public class QueueBindingEntity :
    ExchangeToQueueBinding,
    QueueBindingHandle
{
    readonly ExchangeEntity _exchange;
    readonly QueueEntity _queue;

    /// <summary>Creates a queue-binding entity.</summary>
    /// <param name="id">The topology-local entity identifier.</param>
    /// <param name="exchange">The source exchange.</param>
    /// <param name="queue">The destination queue.</param>
    /// <param name="routingKey">The routing key used by the binding.</param>
    /// <param name="arguments">The broker-specific binding arguments.</param>
    public QueueBindingEntity(long id, ExchangeEntity exchange, QueueEntity queue, string routingKey, IDictionary<string, object?> arguments)
    {
        Id = id;
        RoutingKey = routingKey;
        Arguments = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(arguments ?? new Dictionary<string, object?>()));
        _exchange = exchange;
        _queue = queue;
    }

    /// <summary>Gets a comparer that includes the exchange, queue, routing key, and all binding arguments.</summary>
    public static IEqualityComparer<QueueBindingEntity> EntityComparer { get; } = new QueueBindingEntityEqualityComparer();

    /// <summary>Gets the source exchange.</summary>
    public Exchange Source => _exchange.Exchange;
    /// <summary>Gets the destination queue.</summary>
    public Queue Destination => _queue.Queue;
    /// <summary>Gets the routing key used by the binding.</summary>
    public string RoutingKey { get; }
    /// <summary>Gets the broker-specific binding arguments.</summary>
    public IDictionary<string, object?> Arguments { get; }

    /// <summary>Gets the topology-local entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity as an exchange-to-queue binding declaration.</summary>
    public ExchangeToQueueBinding Binding => this;

    /// <summary>Formats the exchange, queue, routing key, and binding arguments for diagnostics.</summary>
    /// <returns>A diagnostic description of the binding.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"source: {Source.ExchangeName}",
                $"destination: {Destination.QueueName}",
                string.IsNullOrWhiteSpace(RoutingKey) ? "" : $"routing-key: {RoutingKey}",
                string.Join(", ", Arguments.Select(x => $"{x.Key}: {x.Value}"))
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class QueueBindingEntityEqualityComparer : IEqualityComparer<QueueBindingEntity>
    {
        public bool Equals(QueueBindingEntity? x, QueueBindingEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            return x._exchange.Equals(y._exchange) && x._queue.Equals(y._queue) && string.Equals(x.RoutingKey, y.RoutingKey)
                && x.Arguments.Count == y.Arguments.Count
                && x.Arguments.All(a => y.Arguments.TryGetValue(a.Key, out var value) && Equals(a.Value, value));
        }

        public int GetHashCode(QueueBindingEntity obj)
        {
            unchecked
            {
                var hashCode = obj._exchange.GetHashCode();
                hashCode = (hashCode * 397) ^ obj._queue.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.RoutingKey.GetHashCode();
                foreach (KeyValuePair<string, object?> keyValuePair in obj.Arguments.OrderBy(x => x.Key, System.StringComparer.Ordinal))
                {
                    hashCode = (hashCode * 397) ^ keyValuePair.Key.GetHashCode();
                    hashCode = (hashCode * 397) ^ (keyValuePair.Value?.GetHashCode() ?? 0);
                }

                return hashCode;
            }
        }
    }
}
