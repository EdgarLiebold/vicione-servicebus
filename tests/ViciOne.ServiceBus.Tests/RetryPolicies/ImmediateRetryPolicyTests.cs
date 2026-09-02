using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class ImmediateRetryPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "immediate-rejects-invalid-construction")]
    public void Constructor_RejectsANullFilterAndNonPositiveRetryLimit()
    {
        Assert.Throws<ArgumentNullException>(() => new ImmediateRetryPolicy(null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImmediateRetryPolicy(Retry.All(), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImmediateRetryPolicy(Retry.All(), -1));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "immediate-budget-is-exact")]
    public void PolicyContext_ExposesExactlyTheConfiguredImmediateRetryBudget()
    {
        IRetryPolicy policy = Retry.Immediate(2);
        var failure = new RetryFailureException();
        using RetryPolicyContext<TestPipeContext> policyContext = policy.CreatePolicyContext(new TestPipeContext());

        Assert.True(policyContext.CanRetry(failure, out RetryContext<TestPipeContext> first));
        Assert.Equal((0, 1, null), (first.RetryCount, first.RetryAttempt, first.Delay));
        Assert.True(first.CanRetry(failure, out RetryContext<TestPipeContext> second));
        Assert.Equal((1, 2, null), (second.RetryCount, second.RetryAttempt, second.Delay));
        Assert.False(second.CanRetry(failure, out RetryContext<TestPipeContext> terminal));
        Assert.Equal((2, 3, null), (terminal.RetryCount, terminal.RetryAttempt, terminal.Delay));
    }

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RetryFailureException : Exception;
}
