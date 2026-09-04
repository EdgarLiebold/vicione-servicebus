using System.Transactions;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transactions;

public sealed class TransactionFilterTestDriver
{
    private readonly RecordingTransactionContextFactory _factory = new();
    private readonly TransactionFilter<DriverContext> _filter;

    public TransactionFilterTestDriver(IsolationLevel isolationLevel, TimeSpan timeout)
    {
        _filter = new TransactionFilter<DriverContext>(isolationLevel, timeout, _factory);
    }

    public TransactionOptionsSnapshot? CapturedOptions => _factory.CapturedOptions;

    public int CreatedContextCount => _factory.CreatedContextCount;

    public TransactionLifecycleSnapshot? Lifecycle => _factory.Context?.Snapshot;

    public Task ExecuteAsync(Func<TransactionContext, Task> downstream)
    {
        ArgumentNullException.ThrowIfNull(downstream);

        var context = new DriverContext();
        return _filter.SendAsync(
            context,
            Pipe.ExecuteAsync<DriverContext>(current => downstream(current.GetPayload<TransactionContext>())));
    }

    public Task ExecuteNestedAsync(Func<TransactionContext, Task> downstream)
    {
        ArgumentNullException.ThrowIfNull(downstream);

        var context = new DriverContext();
        return _filter.SendAsync(
            context,
            Pipe.ExecuteAsync<DriverContext>(outerContext =>
                _filter.SendAsync(
                    outerContext,
                    Pipe.ExecuteAsync<DriverContext>(innerContext =>
                        downstream(innerContext.GetPayload<TransactionContext>())))));
    }

    public Task ExecuteWithExistingAsync(TransactionContext existing, Func<TransactionContext, Task> downstream)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(downstream);

        var context = new DriverContext();
        context.GetOrAddPayload(() => existing);
        return _filter.SendAsync(
            context,
            Pipe.ExecuteAsync<DriverContext>(current => downstream(current.GetPayload<TransactionContext>())));
    }

    public static object CreateWithNullFactory() =>
        new TransactionFilter<DriverContext>(IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(5), null!);

    public static Task ExecuteWithNullFactoryResultAsync()
    {
        var filter = new TransactionFilter<DriverContext>(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromSeconds(5),
            new NullTransactionContextFactory());
        return filter.SendAsync(new DriverContext(), Pipe.Empty<DriverContext>());
    }

    private sealed class DriverContext : BasePipeContext
    {
    }

    private sealed class RecordingTransactionContextFactory : ITransactionContextFactory
    {
        public TransactionOptionsSnapshot? CapturedOptions { get; private set; }

        public int CreatedContextCount { get; private set; }

        public RecordingManagedTransactionContext? Context { get; private set; }

        public IManagedTransactionContext Create(TransactionOptions options)
        {
            CapturedOptions = new TransactionOptionsSnapshot(options.IsolationLevel, options.Timeout);
            CreatedContextCount++;
            Context = new RecordingManagedTransactionContext();
            return Context;
        }
    }

    private sealed class NullTransactionContextFactory : ITransactionContextFactory
    {
        public IManagedTransactionContext Create(TransactionOptions options) => null!;
    }

    private sealed class RecordingManagedTransactionContext : IManagedTransactionContext
    {
        private readonly CommittableTransaction _transaction = new();
        private int _active = 1;
        private int _commitCount;
        private int _disposeCount;
        private int _rollbackCount;

        public Exception? RollbackException { get; private set; }

        public Transaction Transaction => _transaction;

        public bool IsActive => Volatile.Read(ref _active) == 1;

        public TransactionLifecycleSnapshot Snapshot => new(
            Volatile.Read(ref _commitCount),
            Volatile.Read(ref _rollbackCount),
            Volatile.Read(ref _disposeCount),
            RollbackException);

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _commitCount);
            Volatile.Write(ref _active, 0);
            return Task.CompletedTask;
        }

        public void Rollback()
        {
            Interlocked.Increment(ref _rollbackCount);
            Volatile.Write(ref _active, 0);
        }

        public void Rollback(Exception exception)
        {
            RollbackException = exception;
            Interlocked.Increment(ref _rollbackCount);
            Volatile.Write(ref _active, 0);
        }

        public void Dispose()
        {
            _transaction.Dispose();
            Interlocked.Increment(ref _disposeCount);
            Volatile.Write(ref _active, 0);
        }
    }
}


public sealed record TransactionOptionsSnapshot(IsolationLevel IsolationLevel, TimeSpan Timeout);


public sealed record TransactionLifecycleSnapshot(
    int CommitCount,
    int RollbackCount,
    int DisposeCount,
    Exception? RollbackException);
