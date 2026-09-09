using System;
using System.Threading.Tasks;
using System.Transactions;

namespace ViciOne.ServiceBus.Context;

internal sealed class SystemTransactionContext :
    IManagedTransactionContext
{
    readonly CommittableTransaction _transaction;
    bool _completed;
    bool _disposed;

    public SystemTransactionContext(TransactionOptions options)
    {
        _transaction = new CommittableTransaction(options);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _transaction.Dispose();

        _disposed = true;
    }

    public Transaction Transaction => _transaction;

    public bool IsActive => !_completed && !_disposed;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_completed)
            return;

        await Task.Factory.FromAsync(_transaction.BeginCommit, _transaction.EndCommit, null).ConfigureAwait(false);

        _completed = true;
    }

    public void Rollback()
    {
        if (_completed)
            return;

        _transaction.Rollback();

        _completed = true;
    }

    public void Rollback(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_completed)
            return;

        _transaction.Rollback(exception);

        _completed = true;
    }
}
