using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a saga repository registration configurator implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class SagaRepositoryRegistrationConfigurator<TSaga> :
    ISagaRepositoryRegistrationConfigurator<TSaga>
    where TSaga : class, ISaga
{
    readonly IServiceCollection _collection;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    public SagaRepositoryRegistrationConfigurator(IServiceCollection collection)
    {
        _collection = collection;
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<ServiceDescriptor> GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_collection).GetEnumerator();
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    public void Add(ServiceDescriptor item)
    {
        _collection.Add(item);
    }

    /// <summary>
    /// Performs the clear operation.
    /// </summary>
    public void Clear()
    {
        _collection.Clear();
    }

    /// <summary>
    /// Performs the contains operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Contains(ServiceDescriptor item)
    {
        return _collection.Contains(item);
    }

    /// <summary>
    /// Performs the copy to operation.
    /// </summary>
    /// <param name="array">The array value.</param>
    /// <param name="arrayIndex">The array index value.</param>
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        _collection.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Performs the remove operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Remove(ServiceDescriptor item)
    {
        return _collection.Remove(item);
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _collection.Count;

    /// <summary>
    /// Gets the is read only value.
    /// </summary>
    public bool IsReadOnly => _collection.IsReadOnly;

    /// <summary>
    /// Performs the index of operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    /// <returns>The result of the operation.</returns>
    public int IndexOf(ServiceDescriptor item)
    {
        return _collection.IndexOf(item);
    }

    /// <summary>
    /// Performs the insert operation.
    /// </summary>
    /// <param name="index">The index value.</param>
    /// <param name="item">The item value.</param>
    public void Insert(int index, ServiceDescriptor item)
    {
        _collection.Insert(index, item);
    }

    /// <summary>
    /// Performs the remove at operation.
    /// </summary>
    /// <param name="index">The index value.</param>
    public void RemoveAt(int index)
    {
        _collection.RemoveAt(index);
    }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="index">The index value.</param>
    public ServiceDescriptor this[int index]
    {
        get => _collection[index];
        set => _collection[index] = value;
    }
}
