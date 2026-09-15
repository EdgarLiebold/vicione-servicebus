using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class BaseRetryPolicyContextTests
{
    [Theory]
    [InlineData(RetryPolicyKind.Immediate)]
    [InlineData(RetryPolicyKind.Interval)]
    [InlineData(RetryPolicyKind.Incremental)]
    [InlineData(RetryPolicyKind.Exponential)]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "each-decision-exposes-its-triggering-failure")]
    public void SubsequentDecision_ExposesTheLatestFailure(RetryPolicyKind policyKind)
    {
        IRetryPolicy policy = CreatePolicy(policyKind, 2);
        var initialFailure = new RetryFailureException("initial");
        var latestFailure = new RetryFailureException("latest");
        using RetryPolicyContext<TestPipeContext> context = policy.CreatePolicyContext(new TestPipeContext());

        Assert.True(context.CanRetry(initialFailure, out RetryContext<TestPipeContext> first));
        Assert.True(first.CanRetry(latestFailure, out RetryContext<TestPipeContext> second));
        Assert.Same(latestFailure, second.Exception);
    }

    [Theory]
    [InlineData(RetryPolicyKind.Immediate)]
    [InlineData(RetryPolicyKind.Interval)]
    [InlineData(RetryPolicyKind.Incremental)]
    [InlineData(RetryPolicyKind.Exponential)]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "terminal-decision-has-no-retry-delay")]
    public void ExhaustedDecision_HasNoRetryDelay(RetryPolicyKind policyKind)
    {
        IRetryPolicy policy = CreatePolicy(policyKind, 1);
        using RetryPolicyContext<TestPipeContext> context = policy.CreatePolicyContext(new TestPipeContext());

        Assert.True(context.CanRetry(new RetryFailureException("initial"), out RetryContext<TestPipeContext> first));
        Assert.False(first.CanRetry(new RetryFailureException("terminal"), out RetryContext<TestPipeContext> terminal));
        Assert.Null(terminal.Delay);
    }

    [Theory]
    [InlineData(RetryPolicyKind.Immediate)]
    [InlineData(RetryPolicyKind.Interval)]
    [InlineData(RetryPolicyKind.Incremental)]
    [InlineData(RetryPolicyKind.Exponential)]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "unhandled-initial-decision-has-no-retry-delay")]
    public void UnhandledInitialDecision_HasNoRetryDelay(RetryPolicyKind policyKind)
    {
        IExceptionFilter filter = Retry.Selected<HandledRetryFailureException>();
        IRetryPolicy policy = CreatePolicy(policyKind, 1, filter);
        var failure = new RetryFailureException();
        using RetryPolicyContext<TestPipeContext> context = policy.CreatePolicyContext(new TestPipeContext());

        Assert.False(context.CanRetry(failure, out RetryContext<TestPipeContext> terminal));
        Assert.Same(failure, terminal.Exception);
        Assert.Null(terminal.Delay);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "explicit-cancel-prevents-subsequent-retry")]
    public void Cancel_BeforeTheFirstFailurePreventsEverySubsequentRetry()
    {
        IRetryPolicy policy = Retry.Immediate(2);
        using RetryPolicyContext<TestPipeContext> context = policy.CreatePolicyContext(new TestPipeContext());

        context.Cancel();

        Assert.False(context.CanRetry(new RetryFailureException(), out RetryContext<TestPipeContext> retry));
        Assert.True(retry.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "source-cancellation-propagates-to-retry-context")]
    public void SourceCancellation_PropagatesToAnAlreadyCreatedRetryContext()
    {
        using var cancellation = new CancellationTokenSource();
        IRetryPolicy policy = Retry.Immediate(2);
        using RetryPolicyContext<TestPipeContext> context =
            policy.CreatePolicyContext(new TestPipeContext(cancellation.Token));
        Assert.True(context.CanRetry(new RetryFailureException(), out RetryContext<TestPipeContext> retry));

        cancellation.Cancel();

        Assert.True(retry.CancellationToken.IsCancellationRequested);
    }

    private sealed class TestPipeContext(CancellationToken cancellationToken = default) : BasePipeContext(cancellationToken);

    private static IRetryPolicy CreatePolicy(RetryPolicyKind policyKind, int retryLimit)
    {
        return CreatePolicy(policyKind, retryLimit, Retry.All());
    }

    private static IRetryPolicy CreatePolicy(RetryPolicyKind policyKind, int retryLimit, IExceptionFilter filter)
    {
        return policyKind switch
        {
            RetryPolicyKind.Immediate => filter.Immediate(retryLimit),
            RetryPolicyKind.Interval => filter.Interval(retryLimit, TimeSpan.Zero),
            RetryPolicyKind.Incremental => filter.Incremental(retryLimit, TimeSpan.Zero, TimeSpan.Zero),
            RetryPolicyKind.Exponential => filter.Exponential(retryLimit, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromTicks(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(policyKind), policyKind, "Unknown retry policy kind.")
        };
    }

    public enum RetryPolicyKind
    {
        Immediate,
        Interval,
        Incremental,
        Exponential
    }

    private sealed class RetryFailureException(string message = "retry failure") : Exception(message);

    private sealed class HandledRetryFailureException : Exception;
}
