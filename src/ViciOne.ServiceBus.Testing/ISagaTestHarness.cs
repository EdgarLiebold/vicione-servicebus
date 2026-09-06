using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by saga test harness.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Gets the consumed.</summary>
    IReceivedMessageList Consumed { get; }
    /// <summary>Gets the sagas.</summary>
    ISagaList<TSaga> Sagas { get; }
    /// <summary>Gets the created.</summary>
    ISagaList<TSaga> Created { get; }

    /// <summary>Waits until a saga exists with the specified correlationId.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
    Task<Guid?> ExistsAsync(Guid correlationId, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Waits until at least one saga exists matching the specified filter.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the match outcome.</returns>
    Task<IList<Guid>> MatchAsync(Expression<Func<TSaga, bool>> filter, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Waits until the saga matching the specified correlationId does NOT exist.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the not exists outcome.</returns>
    Task<Guid?> NotExistsAsync(Guid correlationId, TimeSpan? timeout = default, CancellationToken cancellationToken = default);
}
