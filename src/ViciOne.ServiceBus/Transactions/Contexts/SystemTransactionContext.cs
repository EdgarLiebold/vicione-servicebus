using System;
using System.Threading.Tasks;
using System.Transactions;

namespace ViciOne.ServiceBus.Transactions;

internal sealed class SystemTransactionContext :
    IManagedTransactionContext
{
    readonly CommittableTransaction _transaction;
    bool _completed;
    bool _disposed;

    internal SystemTransactionContext(TransactionOptions options)
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (_completed)
            return;

        await Task.Factory.FromAsync(_transaction.BeginCommit, _transaction.EndCommit, null).ConfigureAwait(false);

        _completed = true;
    }

    public void Rollback()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_completed)
            return;

        _transaction.Rollback();

        _completed = true;
    }

    public void Rollback(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_completed)
            return;

        _transaction.Rollback(exception);

        _completed = true;
    }
}
