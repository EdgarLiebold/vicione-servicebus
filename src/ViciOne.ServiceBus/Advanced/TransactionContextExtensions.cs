using System;
using System.Transactions;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates transaction scopes from the transaction carried by a pipe context.</summary>
public static class TransactionContextExtensions
{
    /// <summary>
    /// Creates an asynchronously flowing scope for the transaction carried by the pipe context.
    /// </summary>
    /// <param name="context">The pipe context carrying the transaction.</param>
    /// <returns>The created transaction scope.</returns>
    public static TransactionScope CreateTransactionScope(this PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var transactionContext = context.GetPayload<ITransactionContext>();

        return new TransactionScope(transactionContext.Transaction, TransactionScopeAsyncFlowOption.Enabled);
    }

    /// <summary>
    /// Creates an asynchronously flowing scope with a timeout for the transaction carried by the pipe context.
    /// </summary>
    /// <param name="context">The pipe context carrying the transaction.</param>
    /// <param name="scopeTimeout">The duration after which the scope aborts the transaction.</param>
    /// <returns>The created transaction scope.</returns>
    public static TransactionScope CreateTransactionScope(this PipeContext context, TimeSpan scopeTimeout)
    {
        ArgumentNullException.ThrowIfNull(context);
        var transactionContext = context.GetPayload<ITransactionContext>();

        return new TransactionScope(transactionContext.Transaction, scopeTimeout, TransactionScopeAsyncFlowOption.Enabled);
    }

    /// <summary>
    /// Creates a scope with explicit timeout and async-flow behavior for the transaction carried by the pipe context.
    /// </summary>
    /// <param name="context">The pipe context carrying the transaction.</param>
    /// <param name="scopeTimeout">The duration after which the scope aborts the transaction.</param>
    /// <param name="asyncFlowOptions">Specifies whether transaction flow across thread continuations is enabled.</param>
    /// <returns>The created transaction scope.</returns>
    public static TransactionScope CreateTransactionScope(this PipeContext context, TimeSpan scopeTimeout, TransactionScopeAsyncFlowOption asyncFlowOptions)
    {
        ArgumentNullException.ThrowIfNull(context);
        var transactionContext = context.GetPayload<ITransactionContext>();

        return new TransactionScope(transactionContext.Transaction, scopeTimeout, asyncFlowOptions);
    }
}
