using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Indexes exact saga wrapper references by the property values captured at registration.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
/// <typeparam name="TProperty">The registered key type, including nullable values at runtime.</typeparam>
public class IndexedSagaProperty<TSaga, TProperty> :
    IIndexedSagaProperty<TSaga>,
    IStagedSagaIndex<TSaga>
    where TSaga : class, ISaga
{
    static readonly bool StableHashKey = HasStableHashKey();
    readonly Func<TSaga, TProperty?> _getProperty;
    readonly object _lock = new();
    readonly HashSet<SagaInstance<TSaga>> _pendingAdds = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<SagaInstance<TSaga>> _nullValues = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<SagaInstance<TSaga>, TProperty?> _registrations = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<object, HashSet<SagaInstance<TSaga>>> _values = new();

    /// <summary>Creates an index for a required readable instance property belonging to the saga type.</summary>
    /// <param name="propertyInfo">The non-indexer property whose type matches the registered key type.</param>
    public IndexedSagaProperty(PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        if (propertyInfo.DeclaringType == null || !propertyInfo.DeclaringType.IsAssignableFrom(typeof(TSaga))
            || propertyInfo.PropertyType != typeof(TProperty) || propertyInfo.GetMethod == null
            || propertyInfo.GetMethod.IsStatic || propertyInfo.GetIndexParameters().Length != 0)
            throw new ArgumentException("The saga index requires a readable instance property of the matching key type.", nameof(propertyInfo));

        _getProperty = CreateGetter(propertyInfo);
    }

    internal IndexedSagaProperty(Func<TSaga, TProperty?> getProperty)
    {
        _getProperty = getProperty ?? throw new ArgumentNullException(nameof(getProperty));
    }

    /// <summary>Gets the number of distinct registered keys, including a retained null key.</summary>
    /// <remarks>Unknown key types are compared from a snapshot so mutable key-object hashes cannot strand buckets.</remarks>
    public int Count
    {
        get
        {
            if (!StableHashKey)
                return SnapshotRegistrations().Select(entry => entry.Value).Distinct().Count();

            lock (_lock)
                return _values.Count + (_nullValues.Count == 0 ? 0 : 1);
        }
    }

    /// <summary>Gets the sole wrapper with the registered key, or null when it is absent.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <remarks>Multiple wrappers with the same key are ambiguous and raise InvalidOperationException.</remarks>
    public SagaInstance<TSaga>? this[object? key]
    {
        get => SnapshotKey(key).SingleOrDefault();
    }

    /// <summary>Captures a required wrapper's key and registers that exact reference once.</summary>
    /// <param name="instance">The required wrapper whose state remains referenced without copying.</param>
    /// <remarks>Registered references do not repeat getters; concurrent or reentrant admission of the same pending reference is rejected without waiting.</remarks>
    public void Add(SagaInstance<TSaga> instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        lock (_lock)
        {
            if (_registrations.ContainsKey(instance))
                return;
            if (!_pendingAdds.Add(instance))
                throw new InvalidOperationException("A saga index registration is already in progress for this wrapper.");
        }

        try
        {
            ((IStagedSagaIndex<TSaga>)this).Capture(instance).Apply();
        }
        finally
        {
            lock (_lock)
                _pendingAdds.Remove(instance);
        }
    }

    /// <summary>Removes only the required registered reference, using its captured key without another getter.</summary>
    /// <param name="instance">The required wrapper to remove, or an unregistered reference to leave unchanged.</param>
    /// <remarks>Does not invalidate the wrapper or remove a different equal state.</remarks>
    public void Remove(SagaInstance<TSaga> instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        lock (_lock)
        {
            if (!_registrations.Remove(instance, out TProperty? key) || !StableHashKey)
                return;

            if (key is null)
                _nullValues.Remove(instance);
            else if (_values.TryGetValue(key, out HashSet<SagaInstance<TSaga>>? bucket))
            {
                bucket.Remove(instance);
                if (bucket.Count == 0)
                    _values.Remove(key);
            }
        }
    }

    /// <summary>Evaluates a required predicate against a snapshot of exact registered state references.</summary>
    /// <param name="filter">The required predicate, invoked outside the index lock.</param>
    /// <returns>The materialized matching wrapper references.</returns>
    public IEnumerable<SagaInstance<TSaga>> Where(Func<TSaga, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return SnapshotRegistrations().Select(entry => entry.Key).Where(instance => filter(instance.Instance)).ToArray();
    }

    /// <summary>Evaluates a required predicate against wrappers selected by their registered key.</summary>
    /// <param name="key">The captured registration key to match; null is supported for nullable key types.</param>
    /// <param name="filter">The required predicate, invoked outside the index lock.</param>
    /// <returns>The materialized matching wrapper references.</returns>
    public IEnumerable<SagaInstance<TSaga>> Where(object? key, Func<TSaga, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return SnapshotKey(key).Where(instance => filter(instance.Instance)).ToArray();
    }

    /// <summary>Transforms a membership snapshot outside the index lock and materializes its results.</summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="transformer">The required transformation applied to each referenced state.</param>
    /// <returns>The materialized transformation values without copying saga states.</returns>
    public IEnumerable<TResult> Select<TResult>(Func<TSaga, TResult> transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);
        return SnapshotRegistrations().Select(entry => transformer(entry.Key.Instance)).ToArray();
    }

    internal bool Contains(SagaInstance<TSaga> instance)
    {
        lock (_lock)
            return _registrations.ContainsKey(instance);
    }

    internal int RegistrationCount
    {
        get
        {
            lock (_lock)
                return _registrations.Count;
        }
    }

    internal KeyValuePair<SagaInstance<TSaga>, TProperty?>[] SnapshotRegistrations()
    {
        lock (_lock)
            return _registrations.ToArray();
    }

    SagaIndexRegistration IStagedSagaIndex<TSaga>.Capture(SagaInstance<TSaga> instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        TProperty? key = _getProperty(instance.Instance);
        return new SagaIndexRegistration(key, () => AddCaptured(instance, key), () => Remove(instance));
    }

    bool AddCaptured(SagaInstance<TSaga> instance, TProperty? key)
    {
        lock (_lock)
        {
            if (!_registrations.TryAdd(instance, key))
                return false;

            if (!StableHashKey)
                return true;

            HashSet<SagaInstance<TSaga>>? bucket = null;
            bool addedBucket = false;
            try
            {
                if (key is null)
                    bucket = _nullValues;
                else if (!_values.TryGetValue(key, out bucket))
                {
                    bucket = new HashSet<SagaInstance<TSaga>>(ReferenceEqualityComparer.Instance);
                    _values.Add(key, bucket);
                    addedBucket = true;
                }
                bucket.Add(instance);
                return true;
            }
            catch
            {
                _registrations.Remove(instance);
                bucket?.Remove(instance);
                if (addedBucket && key is not null)
                    _values.Remove(key);
                throw;
            }
        }
    }

    SagaInstance<TSaga>[] SnapshotKey(object? key)
    {
        TProperty? typedKey = RequireKey(key);
        if (!StableHashKey)
            return SnapshotRegistrations().Where(entry => EqualityComparer<TProperty?>.Default.Equals(entry.Value, typedKey)).Select(entry => entry.Key).ToArray();

        lock (_lock)
        {
            if (key is null)
                return _nullValues.ToArray();

            return _values.TryGetValue(key, out HashSet<SagaInstance<TSaga>>? bucket) ? bucket.ToArray() : Array.Empty<SagaInstance<TSaga>>();
        }
    }

    static TProperty? RequireKey(object? key)
    {
        if (key is TProperty value)
            return value;

        if (key is null && (!typeof(TProperty).IsValueType || Nullable.GetUnderlyingType(typeof(TProperty)) != null))
            return default;

        if (key is null)
            throw new ArgumentNullException(nameof(key));

        throw new ArgumentException($"The saga index key must be assignable to {typeof(TProperty)}.", nameof(key));
    }

    static bool HasStableHashKey()
    {
        Type type = Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid)
            || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan);
    }

    static Func<TSaga, TProperty?> CreateGetter(PropertyInfo property)
    {
        ParameterExpression state = Expression.Parameter(typeof(TSaga), "state");
        MemberExpression read = Expression.Property(Expression.Convert(state, property.DeclaringType!), property);
        return Expression.Lambda<Func<TSaga, TProperty?>>(read, state).Compile();
    }
}
