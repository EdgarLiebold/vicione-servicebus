using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a queue binding entity implementation.
/// </summary>
public class QueueBindingEntity :
    ExchangeToQueueBinding,
    QueueBindingHandle
{
    readonly ExchangeEntity _exchange;
    readonly QueueEntity _queue;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="arguments">The arguments value.</param>
    public QueueBindingEntity(long id, ExchangeEntity exchange, QueueEntity queue, string routingKey, IDictionary<string, object?> arguments)
    {
        Id = id;
        RoutingKey = routingKey;
        Arguments = arguments ?? new Dictionary<string, object?>();
        _exchange = exchange;
        _queue = queue;
    }

    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<QueueBindingEntity> EntityComparer { get; } = new QueueBindingEntityEqualityComparer();

    /// <summary>
    /// Gets the source value.
    /// </summary>
    public Exchange Source => _exchange.Exchange;
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public Queue Destination => _queue.Queue;
    /// <summary>
    /// Gets the routing key value.
    /// </summary>
    public string RoutingKey { get; }
    /// <summary>
    /// Gets the arguments value.
    /// </summary>
    public IDictionary<string, object?> Arguments { get; }

    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the binding value.
    /// </summary>
    public ExchangeToQueueBinding Binding => this;

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
