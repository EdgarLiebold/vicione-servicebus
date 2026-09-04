using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides an exchange entity implementation.
/// </summary>
public class ExchangeEntity :
    Exchange,
    ExchangeHandle
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="type">The type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="arguments">The arguments value.</param>
    public ExchangeEntity(long id, string name, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments)
    {
        Id = id;
        ExchangeName = name;
        ExchangeType = type;
        Durable = durable;
        AutoDelete = autoDelete;
        ExchangeArguments = arguments ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Gets the name comparer value.
    /// </summary>
    public static IEqualityComparer<ExchangeEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<ExchangeEntity> EntityComparer { get; } = new ExchangeEntityEqualityComparer();

    /// <summary>
    /// Gets the exchange name value.
    /// </summary>
    public string ExchangeName { get; }
    /// <summary>
    /// Gets the exchange type value.
    /// </summary>
    public string ExchangeType { get; }
    /// <summary>
    /// Gets the durable value.
    /// </summary>
    public bool Durable { get; }
    /// <summary>
    /// Gets the auto delete value.
    /// </summary>
    public bool AutoDelete { get; }
    /// <summary>
    /// Gets the exchange arguments value.
    /// </summary>
    public IDictionary<string, object?> ExchangeArguments { get; }
    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the exchange value.
    /// </summary>
    public Exchange Exchange => this;

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
