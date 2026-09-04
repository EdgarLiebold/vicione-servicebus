using System;
using System.Threading.Tasks;
using System.Transactions;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for transaction context.
/// </summary>
public interface TransactionContext
{
    /// <summary>
    /// Returns the current transaction scope, creating a dependent scope if a thread switch
    /// occurred
    /// </summary>
    Transaction Transaction { get; }

    /// <summary>
    /// Complete the transaction scope
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the transaction
    /// </summary>
    void Rollback();

    /// <summary>
    /// Rollback the transaction
    /// </summary>
    /// <param name="exception">The exception that caused the rollback</param>
    void Rollback(Exception exception);
}
