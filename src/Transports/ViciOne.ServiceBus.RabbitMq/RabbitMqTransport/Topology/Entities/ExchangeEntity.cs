using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Represents a de-duplicated exchange declaration in a broker topology.</summary>
public class ExchangeEntity :
    Exchange,
    ExchangeHandle
{
    /// <summary>Creates an exchange entity.</summary>
    /// <param name="id">The topology-local entity identifier.</param>
    /// <param name="name">The exchange name.</param>
    /// <param name="type">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when it is no longer used.</param>
    /// <param name="arguments">The broker-specific declaration arguments.</param>
    public ExchangeEntity(long id, string name, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments)
    {
        Id = id;
        ExchangeName = name;
        ExchangeType = type;
        Durable = durable;
        AutoDelete = autoDelete;
        ExchangeArguments = arguments ?? new Dictionary<string, object?>();
    }

    /// <summary>Gets a comparer that considers only the exchange name.</summary>
    public static IEqualityComparer<ExchangeEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets a comparer that includes all exchange declaration properties.</summary>
    public static IEqualityComparer<ExchangeEntity> EntityComparer { get; } = new ExchangeEntityEqualityComparer();

    /// <summary>Gets the exchange name.</summary>
    public string ExchangeName { get; }
    /// <summary>Gets the RabbitMQ exchange type.</summary>
    public string ExchangeType { get; }
    /// <summary>Gets whether the exchange survives broker restarts.</summary>
    public bool Durable { get; }
    /// <summary>Gets whether RabbitMQ deletes the exchange when it is no longer used.</summary>
    public bool AutoDelete { get; }
    /// <summary>Gets the broker-specific declaration arguments.</summary>
    public IDictionary<string, object?> ExchangeArguments { get; }
    /// <summary>Gets the topology-local entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity as an exchange declaration.</summary>
    public Exchange Exchange => this;

    /// <summary>Formats the exchange properties for diagnostics.</summary>
    /// <returns>A diagnostic description of the exchange.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"name: {ExchangeName}",
                $"type: {ExchangeType}",
                Durable ? "durable" : "",
                AutoDelete ? "auto-delete" : "",
                string.Join(", ", ExchangeArguments.Select(x => $"{x.Key}: {x.Value}"))
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class NameEqualityComparer : IEqualityComparer<ExchangeEntity>
    {
        public bool Equals(ExchangeEntity? x, ExchangeEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            return string.Equals(x.ExchangeName, y.ExchangeName);
        }

        public int GetHashCode(ExchangeEntity obj)
        {
            return obj.ExchangeName.GetHashCode();
        }
    }


    sealed class ExchangeEntityEqualityComparer :
        IEqualityComparer<ExchangeEntity>
    {
        public bool Equals(ExchangeEntity? x, ExchangeEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            return string.Equals(x.ExchangeName, y.ExchangeName) && string.Equals(x.ExchangeType, y.ExchangeType) && x.Durable == y.Durable
                && x.AutoDelete == y.AutoDelete
                && x.ExchangeArguments.Count == y.ExchangeArguments.Count
                && x.ExchangeArguments.All(a => y.ExchangeArguments.TryGetValue(a.Key, out var value) && Equals(a.Value, value));
        }

        public int GetHashCode(ExchangeEntity obj)
        {
            unchecked
            {
                var hashCode = obj.ExchangeName.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.ExchangeType.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Durable.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.AutoDelete.GetHashCode();
                foreach (KeyValuePair<string, object?> keyValuePair in obj.ExchangeArguments.OrderBy(x => x.Key, System.StringComparer.Ordinal))
                {
                    hashCode = (hashCode * 397) ^ keyValuePair.Key.GetHashCode();
                    hashCode = (hashCode * 397) ^ (keyValuePair.Value?.GetHashCode() ?? 0);
                }

                return hashCode;
            }
        }
    }
}
