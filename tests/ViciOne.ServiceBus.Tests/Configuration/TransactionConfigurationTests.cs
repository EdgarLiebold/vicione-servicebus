using System.Transactions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transactions;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class TransactionConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-CONFIGURATION", "exact-options-and-lifecycle")]
    public async Task Filter_UsesTheExactConfiguredOptionsAndOwnsCommitAndDisposalAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = TimeSpan.FromSeconds(17);
        var driver = new TransactionFilterTestDriver(IsolationLevel.Serializable, timeout);
        ITransactionContext? observed = null;

        await driver.ExecuteAsync(context =>
        {
            observed = context;
            return Task.CompletedTask;
        }).WaitAsync(operationTimeout, cancellationToken);

        Assert.NotNull(observed);
        Assert.Equal(new TransactionOptionsSnapshot(IsolationLevel.Serializable, timeout), driver.CapturedOptions);
        Assert.Equal(new TransactionLifecycleSnapshot(1, 0, 1, null), driver.Lifecycle);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-CONFIGURATION", "failure-rolls-back-and-preserves-identity")]
    public async Task Filter_RollsBackAndDisposesBeforeRethrowingTheExactFailureAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var driver = new TransactionFilterTestDriver(IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(5));
        var expected = new InvalidOperationException("expected transaction failure");

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            driver.ExecuteAsync(_ => Task.FromException(expected)).WaitAsync(operationTimeout, cancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(new TransactionLifecycleSnapshot(0, 1, 1, expected), driver.Lifecycle);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-CONFIGURATION", "nested-filter-reuses-active-context")]
    public async Task NestedFilters_ReuseTheActiveTransactionWithoutTakingDuplicateOwnershipAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var driver = new TransactionFilterTestDriver(IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(5));

        await driver.ExecuteNestedAsync(_ => Task.CompletedTask).WaitAsync(operationTimeout, cancellationToken);

        Assert.Equal(1, driver.CreatedContextCount);
        Assert.Equal(new TransactionLifecycleSnapshot(1, 0, 1, null), driver.Lifecycle);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-CONFIGURATION", "existing-context-remains-externally-owned")]
    public async Task ExistingTransactionContext_IsReusedWithoutTakingOwnershipAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var driver = new TransactionFilterTestDriver(IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(5));
        using var existing = new RecordingExternalTransactionContext();
        ITransactionContext? observed = null;

        await driver.ExecuteWithExistingAsync(existing, context =>
        {
            observed = context;
            return Task.CompletedTask;
        }).WaitAsync(operationTimeout, cancellationToken);

        Assert.Same(existing, observed);
        Assert.Equal(0, driver.CreatedContextCount);
        Assert.Equal(0, existing.CommitCount);
        Assert.Equal(0, existing.RollbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-CONFIGURATION", "invalid-public-boundaries")]
    public void Configuration_RejectsNullAndNonPositiveTimeouts()
    {
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            TransactionConfiguratorExtensions.UseTransaction<TestPipeContext>(null!)).ParamName);

        var zero = new TransactionPipeSpecification<TestPipeContext> { Timeout = TimeSpan.Zero };
        var negative = new TransactionPipeSpecification<TestPipeContext> { Timeout = TimeSpan.FromSeconds(-1) };

        Assert.Equal(ValidationResultDisposition.Failure, Assert.Single(zero.Validate()).Disposition);
        Assert.Equal(ValidationResultDisposition.Failure, Assert.Single(negative.Validate()).Disposition);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => zero.Apply(null!)).ParamName);
        Assert.Equal("timeout", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TransactionFilter<TestPipeContext>(timeout: TimeSpan.FromSeconds(-1))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-CONFIGURATION", "internal-factory-fails-closed")]
    public async Task InternalFactoryBoundary_RejectsNullFactoryAndNullFactoryResultAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Assert.Equal("contextFactory", Assert.Throws<ArgumentNullException>(
            TransactionFilterTestDriver.CreateWithNullFactory).ParamName);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransactionFilterTestDriver.ExecuteWithNullFactoryResultAsync()
                .WaitAsync(operationTimeout, cancellationToken));
        Assert.Equal("The transaction context factory returned null.", actual.Message);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class TestPipeContext : BasePipeContext
    {
    }

    private sealed class RecordingExternalTransactionContext : ITransactionContext, IDisposable
    {
        private readonly CommittableTransaction _transaction = new();

        public int CommitCount { get; private set; }

        public int RollbackCount { get; private set; }

        public Transaction Transaction => _transaction;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); CommitCount++;
            return Task.CompletedTask;
        }

        public void Rollback() => RollbackCount++;

        public void Rollback(Exception exception) => RollbackCount++;

        public void Dispose() => _transaction.Dispose();
    }
}
