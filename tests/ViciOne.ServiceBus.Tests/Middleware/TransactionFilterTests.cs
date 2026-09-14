using System.Transactions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class TransactionFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-SCOPE", "completed-scope-commits-across-await")]
    public async Task CompletedScope_CommitsAndFlowsAcrossAnAsyncContinuationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Transaction? beforeAwait = null;
        Transaction? afterAwait = null;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseTransaction();
            configuration.UseExecuteAwaited(async context =>
            {
                using TransactionScope scope = context.CreateTransactionScope();
                beforeAwait = Transaction.Current;
                await Task.Yield();
                afterAwait = Transaction.Current;
                scope.Complete();
            });
        });

        await pipe.SendAsync(new TestPipeContext()).WaitAsync(timeout, cancellationToken);

        Assert.NotNull(beforeAwait);
        Assert.Same(beforeAwait, afterAwait);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-SCOPE", "incomplete-scope-aborts")]
    public async Task IncompleteScope_AbortsTheOwnedTransactionAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseTransaction();
            configuration.UseExecute(context =>
            {
                using TransactionScope scope = context.CreateTransactionScope();
                Assert.NotNull(Transaction.Current);
            });
        });

        await Assert.ThrowsAsync<TransactionAbortedException>(() =>
            pipe.SendAsync(new TestPipeContext()).WaitAsync(timeout, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-SCOPE", "downstream-failure-identity")]
    public async Task DownstreamFailure_IsRethrownWithoutReplacementAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected downstream failure");
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseTransaction();
            configuration.UseExecute(context =>
            {
                using TransactionScope scope = context.CreateTransactionScope();
                scope.Complete();
                throw expected;
            });
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipe.SendAsync(new TestPipeContext()).WaitAsync(timeout, cancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-RETRY", "fresh-owned-context-per-attempt")]
    public async Task Retry_CreatesAFreshTransactionContextForEveryAttemptAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var contexts = new List<ITransactionContext>();
        var expected = new TransactionRetryException("terminal transaction retry failure");
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.Immediate(1));
            configuration.UseTransaction();
            configuration.UseExecute(context =>
            {
                contexts.Add(context.GetPayload<ITransactionContext>());
                throw expected;
            });
        });

        TransactionRetryException actual = await Assert.ThrowsAsync<TransactionRetryException>(() =>
            pipe.SendAsync(new TestPipeContext()).WaitAsync(timeout, cancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(2, contexts.Count);
        Assert.NotSame(contexts[0], contexts[1]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-CONFIGURATION", "invalid-runtime-collaborators")]
    public async Task Filter_RejectsNullRuntimeCollaboratorsAtTheirExactBoundariesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var filter = new TransactionFilter<TestPipeContext>();

        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(null!, Pipe.Empty<TestPipeContext>()).WaitAsync(timeout, cancellationToken))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(new TestPipeContext(), null!).WaitAsync(timeout, cancellationToken))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            ((IProbeSite)filter).Probe(null!)).ParamName);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class TestPipeContext : BasePipeContext
    {
        public TestPipeContext()
        {
        }

        public TestPipeContext(CancellationToken cancellationToken)
            : base(cancellationToken)
        {
        }
    }

    private sealed class TransactionRetryException(string message) : Exception(message);
}
