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

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RetryFailureException : Exception;
}
