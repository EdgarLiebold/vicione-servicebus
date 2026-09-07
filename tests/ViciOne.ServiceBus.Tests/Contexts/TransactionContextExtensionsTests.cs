using System.Transactions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contexts;

public sealed class TransactionContextExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-SCOPE", "explicit-async-flow-option")]
    public async Task ExplicitAsyncFlowOption_PreservesTheAmbientTransactionAcrossAwaitAsync()
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
                using TransactionScope scope = context.CreateTransactionScope(
                    TimeSpan.FromSeconds(5),
                    TransactionScopeAsyncFlowOption.Enabled);
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
    [RequirementCoverage("REQ-VSB-TRANSACTION-SCOPE", "timeout-overload-enables-async-flow")]
    public async Task TimeoutOverload_PreservesTheAmbientTransactionAcrossAwaitAsync()
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
                using TransactionScope scope = context.CreateTransactionScope(TimeSpan.FromSeconds(5));
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

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSACTION-SCOPE", "null-context-boundaries")]
    public void ScopeFactories_RejectNullContextsAtEveryPublicOverload()
    {
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            TransactionContextExtensions.CreateTransactionScope(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            TransactionContextExtensions.CreateTransactionScope(null!, TimeSpan.FromSeconds(1))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            TransactionContextExtensions.CreateTransactionScope(
                null!,
                TimeSpan.FromSeconds(1),
                TransactionScopeAsyncFlowOption.Enabled)).ParamName);
    }

    private sealed class TestPipeContext : BasePipeContext
    {
    }
}
