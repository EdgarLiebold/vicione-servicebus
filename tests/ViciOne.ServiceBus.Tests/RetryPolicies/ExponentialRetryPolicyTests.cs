using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class ExponentialRetryPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "exponential-rejects-invalid-construction")]
    public void Constructor_RejectsInvalidLimitsBoundsAndDelta()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ExponentialRetryPolicy(null!, 1, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialRetryPolicy(Retry.All(), 0, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialRetryPolicy(Retry.All(), 1, TimeSpan.FromTicks(-1), TimeSpan.Zero, TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialRetryPolicy(Retry.All(), 1, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialRetryPolicy(Retry.All(), 1, TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialRetryPolicy(Retry.All(), 1, TimeSpan.Zero,
                TimeSpan.FromMilliseconds((double)int.MaxValue + 1), TimeSpan.FromSeconds(1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "exponential-rejects-negative-attempt")]
    public void GetRetryInterval_RejectsANegativeRetryCount()
    {
        var policy = new ExponentialRetryPolicy(
            Retry.All(), 1, TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => policy.GetRetryInterval(-1));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "exponential-attempt-delay-is-stable-and-bounded")]
    public void PolicyContext_SelectsOneStableBoundedDelayPerAttempt()
    {
        TimeSpan minimum = TimeSpan.FromSeconds(1);
        TimeSpan maximum = TimeSpan.FromSeconds(10);
        var policy = new ExponentialRetryPolicy(
            Retry.All(), 10, minimum, maximum, TimeSpan.FromSeconds(2));
        using RetryPolicyContext<TestPipeContext> policyContext =
            ((IRetryPolicy)policy).CreatePolicyContext(new TestPipeContext());

        Assert.True(policyContext.CanRetry(new RetryFailureException(), out RetryContext<TestPipeContext> retry));
        TimeSpan? firstRead = retry.Delay;
        TimeSpan? secondRead = retry.Delay;

        Assert.Equal(firstRead, secondRead);
        Assert.InRange(firstRead!.Value, minimum, maximum);
        Assert.Equal(0, retry.RetryCount);
        Assert.Equal(1, retry.RetryAttempt);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "exponential-limit-is-exact")]
    public async Task RetryFilter_ExecutesExactlyTheConfiguredExponentialBudgetAsync()
    {
        const int retryLimit = 10;
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.Exponential(
                retryLimit, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromTicks(1)));
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new RetryFailureException();
            });
        });

        await Assert.ThrowsAsync<RetryFailureException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(retryLimit + 1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "exponential-preserves-fractional-minimum")]
    public void FractionalMinimum_IsPreservedAtTickPrecision()
    {
        TimeSpan minimum = TimeSpan.FromTicks(9_000);
        TimeSpan maximum = TimeSpan.FromTicks(13_000);
        IRetryPolicy policy = Retry.Exponential(2, minimum, maximum, TimeSpan.FromTicks(1));
        using RetryPolicyContext<TestPipeContext> context = policy.CreatePolicyContext(new TestPipeContext());

        Assert.True(context.CanRetry(new RetryFailureException(), out RetryContext<TestPipeContext> retry));

        Assert.InRange(Assert.IsType<TimeSpan>(retry.Delay), minimum, maximum);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "exponential-positive-tick-delta-reaches-cap")]
    public void PositiveTickDelta_ReachesTheBoundedCapWithoutZeroGrowth()
    {
        TimeSpan maximum = TimeSpan.FromMilliseconds(1);
        IRetryPolicy policy = Retry.Exponential(64, TimeSpan.Zero, maximum, TimeSpan.FromTicks(1));
        using RetryPolicyContext<TestPipeContext> context = policy.CreatePolicyContext(new TestPipeContext());
        var failure = new RetryFailureException();
        Assert.True(context.CanRetry(failure, out RetryContext<TestPipeContext> retry));
        for (int attempt = 1; attempt < 64; attempt++)
        {
            Assert.True(retry.CanRetry(failure, out RetryContext<TestPipeContext> next));
            retry = next;
        }

        Assert.InRange(Assert.IsType<TimeSpan>(retry.Delay), maximum * 0.75, maximum);
        Assert.False(retry.CanRetry(failure, out RetryContext<TestPipeContext> terminal));
        Assert.Null(terminal.Delay);
    }

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RetryFailureException : Exception;
}
