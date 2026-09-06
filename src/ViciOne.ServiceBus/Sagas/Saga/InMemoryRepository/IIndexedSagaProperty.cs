using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// For the in-memory saga repository, this maintains an index of saga properties
/// for fast searching.
/// </summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
public interface IIndexedSagaProperty<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Returns the saga with the specified key.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    SagaInstance<TSaga>? this[object key] { get; }

    /// <summary>Gets the count.</summary>
    int Count { get; }

    /// <summary>Adds a new saga to the index.</summary>
    /// <param name="newItem">The new item.</param>
    void Add(SagaInstance<TSaga> newItem);

    /// <summary>Removes a saga from the index.</summary>
    /// <param name="item">The item.</param>
    void Remove(SagaInstance<TSaga> item);

    /// <summary>Returns sagas matching the filter function.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The enumerable produced by the operation.</returns>
    IEnumerable<SagaInstance<TSaga>> Where(Func<TSaga, bool> filter);

    /// <summary>Returns sagas matching the filter function where the key also matches.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The enumerable produced by the operation.</returns>
    IEnumerable<SagaInstance<TSaga>> Where(object key, Func<TSaga, bool> filter);

    /// <summary>Selects sagas from the index, running the transformation function and returning the output type.</summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="transformer">The transformer.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<TResult> Select<TResult>(Func<TSaga, TResult> transformer);
}
