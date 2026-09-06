using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Represents a de-duplicated exchange-to-exchange binding in a broker topology.</summary>
public class ExchangeBindingEntity :
    ExchangeToExchangeBinding,
    ExchangeBindingHandle
{
    readonly ExchangeEntity _destination;

    readonly ExchangeEntity _source;

    /// <summary>Creates an exchange-binding entity.</summary>
    /// <param name="id">The topology-local entity identifier.</param>
    /// <param name="source">The source exchange.</param>
    /// <param name="destination">The destination exchange.</param>
    /// <param name="routingKey">The routing key used by the binding.</param>
    /// <param name="arguments">The broker-specific binding arguments.</param>
    public ExchangeBindingEntity(long id, ExchangeEntity source, ExchangeEntity destination, string routingKey, IDictionary<string, object?> arguments)
    {
        Id = id;
        RoutingKey = routingKey;
        Arguments = arguments ?? new Dictionary<string, object?>();
        _source = source;
        _destination = destination;
    }

    /// <summary>Gets a comparer that includes both exchanges, the routing key, and all binding arguments.</summary>
    public static IEqualityComparer<ExchangeBindingEntity> EntityComparer { get; } = new ExchangeBindingEntityEqualityComparer();
    /// <summary>Gets the topology-local entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity as an exchange-to-exchange binding declaration.</summary>
    public ExchangeToExchangeBinding Binding => this;

    /// <summary>Gets the source exchange.</summary>
    public Exchange Source => _source.Exchange;
    /// <summary>Gets the destination exchange.</summary>
    public Exchange Destination => _destination.Exchange;
    /// <summary>Gets the routing key used by the binding.</summary>
    public string RoutingKey { get; }
    /// <summary>Gets the broker-specific binding arguments.</summary>
    public IDictionary<string, object?> Arguments { get; }

    /// <summary>Formats the source, destination, routing key, and binding arguments for diagnostics.</summary>
    /// <returns>A diagnostic description of the binding.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"source: {Source.ExchangeName}",
                $"destination: {Destination.ExchangeName}",
                string.IsNullOrWhiteSpace(RoutingKey) ? "" : $"routing-key: {RoutingKey}",
                string.Join(", ", Arguments.Select(x => $"{x.Key}: {x.Value}"))
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class ExchangeBindingEntityEqualityComparer : IEqualityComparer<ExchangeBindingEntity>
    {
        public bool Equals(ExchangeBindingEntity? x, ExchangeBindingEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            return x._source.Equals(y._source) && x._destination.Equals(y._destination) && string.Equals(x.RoutingKey, y.RoutingKey)
                && x.Arguments.Count == y.Arguments.Count
                && x.Arguments.All(a => y.Arguments.TryGetValue(a.Key, out var value) && Equals(a.Value, value));
        }

        public int GetHashCode(ExchangeBindingEntity obj)
        {
            unchecked
            {
                var hashCode = obj._source.GetHashCode();
                hashCode = (hashCode * 397) ^ obj._destination.GetHashCode();
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
