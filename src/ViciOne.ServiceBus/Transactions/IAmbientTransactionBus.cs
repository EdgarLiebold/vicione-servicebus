namespace ViciOne.ServiceBus.Transactions;

/// <summary>
/// A transaction-aware bus facade that defers sends and publishes while <see cref="System.Transactions.Transaction.Current" /> is active.
/// Pending actions execute in FIFO order during transaction preparation, are discarded on rollback or in-doubt completion, and abort
/// preparation when dispatch fails. Caller cancellation is honored before enlistment; after enlistment, the transaction owns the
/// dispatch lifetime. The facade is best-effort and is not a durable atomic outbox.
/// </summary>
public interface IAmbientTransactionBus :
    IBus
{
}
