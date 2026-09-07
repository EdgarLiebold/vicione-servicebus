using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Saga;

/// <summary>A dictionary index of the sagas.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class IndexedSagaProperty<TSaga, TProperty> :
    IIndexedSagaProperty<TSaga>
    where TSaga : class, ISaga
    where TProperty : notnull
{
    readonly Func<TSaga, TProperty> _getProperty;
    readonly IDictionary<TProperty, HashSet<SagaInstance<TSaga>>> _values;

    /// <summary>Creates an index for the specified property of a saga.</summary>
    /// <param name="propertyInfo">The property info.</param>
    public IndexedSagaProperty(PropertyInfo propertyInfo)
    {
        _values = new Dictionary<TProperty, HashSet<SagaInstance<TSaga>>>();
        _getProperty = GetGetMethod(propertyInfo);
    }

    /// <summary>Gets the count.</summary>
    public int Count => _values.Count;

    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    public SagaInstance<TSaga>? this[object key]
    {
        get
        {
            var keyValue = (TProperty)key;

            if (_values.TryGetValue(keyValue, out HashSet<SagaInstance<TSaga>>? result))
                return result.SingleOrDefault();

            return null;
        }
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="newItem">The new item.</param>
    public void Add(SagaInstance<TSaga> newItem)
    {
        var key = _getProperty(newItem.Instance);

        if (!_values.TryGetValue(key, out HashSet<SagaInstance<TSaga>>? hashSet))
        {
            hashSet = new HashSet<SagaInstance<TSaga>>();
            _values.Add(key, hashSet);
        }

        hashSet.Add(newItem);
    }

    /// <summary>Removes the selected value.</summary>
    /// <param name="instance">The instance.</param>
    public void Remove(SagaInstance<TSaga> instance)
    {
        var key = _getProperty(instance.Instance);

        if (!_values.TryGetValue(key, out HashSet<SagaInstance<TSaga>>? hashSet))
            return;

        if (hashSet.Remove(instance) && hashSet.Count == 0)
            _values.Remove(key);
    }

    /// <summary>Filters values using the supplied predicate.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The enumerable produced by the operation.</returns>
    public IEnumerable<SagaInstance<TSaga>> Where(Func<TSaga, bool> filter)
    {
        return _values.Values.SelectMany(x => x).Where(x => filter(x.Instance));
    }

    /// <summary>Filters values using the supplied predicate.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The enumerable produced by the operation.</returns>
    public IEnumerable<SagaInstance<TSaga>> Where(object key, Func<TSaga, bool> filter)
    {
        var keyValue = (TProperty)key;

        if (_values.TryGetValue(keyValue, out HashSet<SagaInstance<TSaga>>? resultSet))
            return resultSet.Where(x => filter(x.Instance));

        return Enumerable.Empty<SagaInstance<TSaga>>();
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="transformer">The transformer.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<TResult> Select<TResult>(Func<TSaga, TResult> transformer)
    {
        return _values.Values.SelectMany(x => x).Select(x => transformer(x.Instance));
    }

    static Func<TSaga, TProperty> GetGetMethod(PropertyInfo property)
    {
        return ReadPropertyCache<TSaga>.GetProperty<TProperty>(property).Get;
    }
}
