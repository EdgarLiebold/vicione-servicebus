using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Indexes exact saga wrapper references by captured registration keys.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
public interface IIndexedSagaProperty<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Gets the sole wrapper for a registered key, or null when the key is absent.</summary>
    /// <param name="key">The registered key, including null for nullable key types.</param>
    SagaInstance<TSaga>? this[object? key] { get; }

    /// <summary>Gets the number of distinct registered keys, not the number of wrappers.</summary>
    int Count { get; }

    /// <summary>Captures a required wrapper's key and retains that exact reference once.</summary>
    /// <param name="instance">The required wrapper whose state is not copied.</param>
    void Add(SagaInstance<TSaga> instance);

    /// <summary>Removes only an actually registered wrapper reference without invalidating its state.</summary>
    /// <param name="instance">The required wrapper reference; an unregistered reference leaves membership unchanged.</param>
    void Remove(SagaInstance<TSaga> instance);

    /// <summary>Evaluates a required predicate against a snapshot of registered state references.</summary>
    /// <param name="filter">The required predicate, invoked outside the index lock.</param>
    /// <returns>The materialized matching wrapper references.</returns>
    IEnumerable<SagaInstance<TSaga>> Where(Func<TSaga, bool> filter);

    /// <summary>Evaluates a required predicate against wrappers selected by their registered key.</summary>
    /// <param name="key">The captured registration key, including null for nullable key types.</param>
    /// <param name="filter">The required predicate, invoked outside the index lock.</param>
    /// <returns>The materialized matching wrapper references.</returns>
    IEnumerable<SagaInstance<TSaga>> Where(object? key, Func<TSaga, bool> filter);

    /// <summary>Transforms a membership snapshot outside the index lock and materializes result values.</summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="transformer">The required transformation applied to referenced state.</param>
    /// <returns>The materialized transformation values without copying saga states.</returns>
    IEnumerable<TResult> Select<TResult>(Func<TSaga, TResult> transformer);
}
