using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures saga repository registration.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaRepositoryRegistrationConfigurator<TSaga> :
    ISagaRepositoryRegistrationConfigurator<TSaga>
    where TSaga : class, ISaga
{
    readonly IServiceCollection _collection;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="collection">The collection.</param>
    public SagaRepositoryRegistrationConfigurator(IServiceCollection collection)
    {
        _collection = collection;
    }

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<ServiceDescriptor> GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_collection).GetEnumerator();
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="item">The item.</param>
    public void Add(ServiceDescriptor item)
    {
        _collection.Add(item);
    }

    /// <summary>Removes every item from the current collection.</summary>
    public void Clear()
    {
        _collection.Clear();
    }

    /// <summary>Determines whether the current collection contains the supplied value.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Contains(ServiceDescriptor item)
    {
        return _collection.Contains(item);
    }

    /// <summary>Copies the current collection to the supplied destination.</summary>
    /// <param name="array">The array.</param>
    /// <param name="arrayIndex">The array index.</param>
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        _collection.CopyTo(array, arrayIndex);
    }

    /// <summary>Removes the selected value.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Remove(ServiceDescriptor item)
    {
        return _collection.Remove(item);
    }

    /// <summary>Gets the count.</summary>
    public int Count => _collection.Count;

    /// <summary>Gets a value indicating whether read only.</summary>
    public bool IsReadOnly => _collection.IsReadOnly;

    /// <summary>Returns the index of the supplied value.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The int produced by the operation.</returns>
    public int IndexOf(ServiceDescriptor item)
    {
        return _collection.IndexOf(item);
    }

    /// <summary>Inserts the supplied value.</summary>
    /// <param name="index">The index.</param>
    /// <param name="item">The item.</param>
    public void Insert(int index, ServiceDescriptor item)
    {
        _collection.Insert(index, item);
    }

    /// <summary>Removes at.</summary>
    /// <param name="index">The index.</param>
    public void RemoveAt(int index)
    {
        _collection.RemoveAt(index);
    }

    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="index">The index.</param>
    public ServiceDescriptor this[int index]
    {
        get => _collection[index];
        set => _collection[index] = value;
    }
}
