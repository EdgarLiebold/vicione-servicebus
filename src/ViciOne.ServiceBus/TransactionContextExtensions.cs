using System;
using System.Transactions;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for transaction context.
/// </summary>
public static class TransactionContextExtensions
{
    /// <summary>
    /// Create a transaction scope using the transaction context (added by the TransactionFilter),
    /// to ensure that any transactions are carried between any threads.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static TransactionScope CreateTransactionScope(this PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var transactionContext = context.GetPayload<TransactionContext>();

        return new TransactionScope(transactionContext.Transaction, TransactionScopeAsyncFlowOption.Enabled);
    }

    /// <summary>
    /// Create a transaction scope using the transaction context (added by the TransactionFilter),
    /// to ensure that any transactions are carried between any threads.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scopeTimeout">The timespan after which the scope times out and aborts the transaction</param>
    /// <returns></returns>
    public static TransactionScope CreateTransactionScope(this PipeContext context, TimeSpan scopeTimeout)
    {
        ArgumentNullException.ThrowIfNull(context);
        var transactionContext = context.GetPayload<TransactionContext>();

        return new TransactionScope(transactionContext.Transaction, scopeTimeout, TransactionScopeAsyncFlowOption.Enabled);
    }

    /// <summary>
    /// Create a transaction scope using the transaction context (added by the TransactionFilter),
    /// to ensure that any transactions are carried between any threads.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scopeTimeout">The timespan after which the scope times out and aborts the transaction</param>
    /// <param name="asyncFlowOptions">Specifies whether transaction flow across thread continuations is enabled.</param>
    /// <returns></returns>
    public static TransactionScope CreateTransactionScope(this PipeContext context, TimeSpan scopeTimeout, TransactionScopeAsyncFlowOption asyncFlowOptions)
    {
        ArgumentNullException.ThrowIfNull(context);
        var transactionContext = context.GetPayload<TransactionContext>();

        return new TransactionScope(transactionContext.Transaction, scopeTimeout, asyncFlowOptions);
    }
}
