using System;
using System.Threading.Tasks;
using System.Transactions;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Controls the transaction owned or exposed by a pipeline context.</summary>
public interface TransactionContext
{
    /// <summary>Gets the transaction used to create nested transaction scopes.</summary>
    Transaction Transaction { get; }

    /// <summary>Commits the active transaction.</summary>
    /// <param name="cancellationToken">Cancels the commit before it begins.</param>
    /// <returns>A task that completes when the transaction manager has committed the transaction.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the active transaction.</summary>
    void Rollback();

    /// <summary>Rolls back the active transaction with its originating failure.</summary>
    /// <param name="exception">The failure that caused the rollback.</param>
    void Rollback(Exception exception);
}
