using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides synchronous snapshots and asynchronous observation of recorded saga instances.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaList<out TSaga> :
    IAsyncElementList<ISagaInstance<TSaga>>
    where TSaga : class, ISaga
{
    /// <summary>Returns a snapshot of saga observations that match the predicate.</summary>
    /// <param name="filter">The saga predicate.</param>
    /// <returns>The matching observations.</returns>
    IReadOnlyList<ISagaInstance<TSaga>> Snapshot(FilterDelegate<TSaga> filter);

    /// <summary>Finds the most recently recorded saga with the specified correlation identifier.</summary>
    /// <param name="sagaId">The saga correlation identifier.</param>
    /// <returns>The recorded saga, or <see langword="null"/> when no match exists.</returns>
    TSaga? FindById(Guid sagaId);

    /// <summary>Observes recorded saga instances as they become available.</summary>
    /// <param name="cancellationToken">The token that cancels observation.</param>
    /// <returns>An asynchronous sequence of saga observations.</returns>
    IAsyncEnumerable<ISagaInstance<TSaga>> SelectAsync(CancellationToken cancellationToken = default);

    /// <summary>Observes recorded saga instances that match a predicate.</summary>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="cancellationToken">The token that cancels observation.</param>
    /// <returns>An asynchronous sequence of matching saga observations.</returns>
    IAsyncEnumerable<ISagaInstance<TSaga>> SelectAsync(FilterDelegate<TSaga> filter, CancellationToken cancellationToken = default);

    /// <summary>Waits for any recorded saga instance.</summary>
    /// <param name="cancellationToken">The token that cancels the wait.</param>
    /// <returns><see langword="true"/> when an observation arrives before the list timeout; otherwise, <see langword="false"/>.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>Waits for a recorded saga instance that matches a predicate.</summary>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="cancellationToken">The token that cancels the wait.</param>
    /// <returns><see langword="true"/> when a match arrives before the list timeout; otherwise, <see langword="false"/>.</returns>
    Task<bool> AnyAsync(FilterDelegate<TSaga> filter, CancellationToken cancellationToken = default);
}
