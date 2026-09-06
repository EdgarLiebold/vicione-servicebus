using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

/// <summary>Defines version-checked Amazon DynamoDB persistence operations for one saga type.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface DatabaseContext<TSaga> :
    IDisposable
    where TSaga : class, ISagaVersion
{
    /// <summary>Creates a saga document only when its composite key does not already exist.</summary>
    /// <param name="instance">The saga state to persist.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional put.</returns>
    Task AddAsync(TSaga instance, CancellationToken cancellationToken);

    /// <summary>Inserts a saga document only when its composite key does not already exist.</summary>
    /// <param name="instance">The saga state to persist.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional put.</returns>
    Task InsertAsync(TSaga instance, CancellationToken cancellationToken);

    /// <summary>Loads and validates a saga document by correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the saga state, or <see langword="null"/> when no document exists.</returns>
    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken);

    /// <summary>Increments the saga version and updates the document only when the persisted version still matches.</summary>
    /// <param name="instance">The saga state to update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional update.</returns>
    Task UpdateAsync(TSaga instance, CancellationToken cancellationToken);

    /// <summary>Deletes a saga document only when the persisted version still matches.</summary>
    /// <param name="instance">The saga state whose document is deleted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional delete.</returns>
    Task DeleteAsync(TSaga instance, CancellationToken cancellationToken);
}
