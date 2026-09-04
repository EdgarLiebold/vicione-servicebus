using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for saga test harness.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ISagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    IReceivedMessageList Consumed { get; }
    /// <summary>
    /// Gets the sagas value.
    /// </summary>
    ISagaList<TSaga> Sagas { get; }
    /// <summary>
    /// Gets the created value.
    /// </summary>
    ISagaList<TSaga> Created { get; }

    /// <summary>
    /// Waits until a saga exists with the specified correlationId
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<Guid?> ExistsAsync(Guid correlationId, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until at least one saga exists matching the specified filter
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<IList<Guid>> MatchAsync(Expression<Func<TSaga, bool>> filter, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until the saga matching the specified correlationId does NOT exist
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<Guid?> NotExistsAsync(Guid correlationId, TimeSpan? timeout = default, CancellationToken cancellationToken = default);
}
