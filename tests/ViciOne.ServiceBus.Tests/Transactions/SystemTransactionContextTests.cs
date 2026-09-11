using System.Transactions;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transactions;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transactions;

public sealed class SystemTransactionContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TRANSACTION-CONTEXT", "factory-active-commit-and-idempotent-completion")]
    public async Task FactoryContext_CommitsOnceAndBecomesInactiveAsync()
    {
        IManagedTransactionContext context = SystemTransactionContextFactory.Instance.Create(new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(5),
        });
        using (context)
        {
            Assert.IsType<SystemTransactionContext>(context);
            Assert.IsType<CommittableTransaction>(context.Transaction);
            Assert.True(context.IsActive);

            await context.CommitAsync(TestContext.Current.CancellationToken);
            await context.CommitAsync(TestContext.Current.CancellationToken);
            context.Rollback();
            context.Rollback(new InvalidOperationException("ignored after commit"));

            Assert.False(context.IsActive);
            Assert.Equal(TransactionStatus.Committed, context.Transaction.TransactionInformation.Status);
        }

        context.Dispose();
        Assert.False(context.IsActive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TRANSACTION-CONTEXT", "rollback-with-cause-and-terminal-disposal")]
    public async Task RollbackAndDisposal_AreTerminalAndRejectSubsequentOperationsAsync()
    {
        var expected = new InvalidOperationException("transaction failed");
        var rolledBack = new SystemTransactionContext(new TransactionOptions
        {
            IsolationLevel = IsolationLevel.Serializable,
            Timeout = TimeSpan.FromSeconds(5),
        });

        rolledBack.Rollback(expected);

        Assert.False(rolledBack.IsActive);
        Assert.Equal(TransactionStatus.Aborted, rolledBack.Transaction.TransactionInformation.Status);
        await rolledBack.CommitAsync(TestContext.Current.CancellationToken);
        rolledBack.Rollback();
        rolledBack.Dispose();

        var disposed = new SystemTransactionContext(new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(5),
        });
        disposed.Dispose();

        Assert.False(disposed.IsActive);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => disposed.CommitAsync(TestContext.Current.CancellationToken));
        Assert.Throws<ObjectDisposedException>(disposed.Rollback);
        Assert.Throws<ObjectDisposedException>(() => disposed.Rollback(expected));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TRANSACTION-CONTEXT", "cancellation-does-not-complete-active-transaction")]
    public async Task CanceledCommit_LeavesTheTransactionActiveForExplicitRollbackAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var context = new SystemTransactionContext(new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(5),
        });

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.CommitAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(context.IsActive);
        context.Rollback();
        Assert.False(context.IsActive);
    }
}
