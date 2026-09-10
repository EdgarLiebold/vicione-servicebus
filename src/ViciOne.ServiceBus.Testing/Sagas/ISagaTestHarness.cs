using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Exposes observed saga activity and repository polling operations.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Gets messages delivered through the saga repository.</summary>
    IConsumedMessageList Consumed { get; }
    /// <summary>Gets saga instances observed by the repository.</summary>
    ISagaList<TSaga> Sagas { get; }
    /// <summary>Gets saga instances created by the repository.</summary>
    ISagaList<TSaga> Created { get; }

    /// <summary>Waits until a saga with the specified correlation identifier exists.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use the harness default.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga is found; otherwise, <see langword="null"/>.</returns>
    Task<Guid?> WaitForSagaAsync(Guid correlationId, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Waits until at least one saga matches the specified predicate.</summary>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use the harness default.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The matching saga correlation identifiers, or an empty list when the timeout expires.</returns>
    Task<IReadOnlyList<Guid>> WaitForSagasAsync(Expression<Func<TSaga, bool>> filter, TimeSpan? timeout = default,
        CancellationToken cancellationToken = default);

    /// <summary>Waits until the saga with the specified correlation identifier does not exist.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use the harness default.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier after absence is confirmed; otherwise, <see langword="null"/>.</returns>
    Task<Guid?> WaitForSagaRemovalAsync(Guid correlationId, TimeSpan? timeout = default, CancellationToken cancellationToken = default);
}
