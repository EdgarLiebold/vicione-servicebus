using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

/// <summary>
/// Defines the contract for database context.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface DatabaseContext<TSaga> :
    IDisposable
    where TSaga : class, ISagaVersion
{
    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task AddAsync(TSaga instance, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the insert operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task InsertAsync(TSaga instance, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task UpdateAsync(TSaga instance, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the delete operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeleteAsync(TSaga instance, CancellationToken cancellationToken);
}
