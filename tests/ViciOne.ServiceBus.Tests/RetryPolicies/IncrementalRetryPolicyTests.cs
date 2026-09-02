using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class IncrementalRetryPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "incremental-rejects-invalid-construction")]
    public void Constructor_RejectsInvalidLimitsIntervalsAndOverflow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new IncrementalRetryPolicy(null!, 1, TimeSpan.Zero, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new IncrementalRetryPolicy(Retry.All(), 0, TimeSpan.Zero, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new IncrementalRetryPolicy(Retry.All(), 1, TimeSpan.FromTicks(-1), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new IncrementalRetryPolicy(Retry.All(), 1, TimeSpan.Zero, TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new IncrementalRetryPolicy(Retry.All(), 2, TimeSpan.MaxValue, TimeSpan.FromTicks(1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "incremental-budget-and-delays-are-exact")]
    public void PolicyContext_AddsTheConfiguredIncrementForEveryAttempt()
    {
        IRetryPolicy policy = Retry.Incremental(3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
        var failure = new RetryFailureException();
        using RetryPolicyContext<TestPipeContext> policyContext = policy.CreatePolicyContext(new TestPipeContext());

        Assert.True(policyContext.CanRetry(failure, out RetryContext<TestPipeContext> first));
        Assert.Equal(TimeSpan.FromSeconds(2), first.Delay);
        Assert.True(first.CanRetry(failure, out RetryContext<TestPipeContext> second));
        Assert.Equal(TimeSpan.FromSeconds(5), second.Delay);
        Assert.True(second.CanRetry(failure, out RetryContext<TestPipeContext> third));
        Assert.Equal(TimeSpan.FromSeconds(8), third.Delay);
        Assert.False(third.CanRetry(failure, out RetryContext<TestPipeContext> terminal));
        Assert.Equal(TimeSpan.FromSeconds(11), terminal.Delay);
    }

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RetryFailureException : Exception;
}
