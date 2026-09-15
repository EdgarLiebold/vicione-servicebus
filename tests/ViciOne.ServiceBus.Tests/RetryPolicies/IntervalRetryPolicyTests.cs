using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class IntervalRetryPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "interval-rejects-invalid-construction")]
    public void Constructor_RejectsNullEmptyAndNegativeSchedules()
    {
        Assert.Throws<ArgumentNullException>(() => new IntervalRetryPolicy(null!, TimeSpan.Zero));
        Assert.Throws<ArgumentNullException>(() => new IntervalRetryPolicy(Retry.All(), (TimeSpan[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalRetryPolicy(Retry.All(), Array.Empty<TimeSpan>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalRetryPolicy(Retry.All(), TimeSpan.FromTicks(-1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "interval-schedule-is-immutable-snapshot")]
    public void TimeSpanSchedule_IsAnImmutableSnapshotOfTheCallerInput()
    {
        TimeSpan[] input = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)];
        var policy = new IntervalRetryPolicy(Retry.All(), input);

        input[0] = TimeSpan.FromDays(1);
        var exposed = Assert.IsAssignableFrom<IList<TimeSpan>>(policy.Intervals);

        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], policy.Intervals);
        Assert.True(exposed.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => exposed[0] = TimeSpan.Zero);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "interval-api-requires-explicit-time-spans")]
    public void PublicApi_RequiresExplicitTimeSpanSchedules()
    {
        Assert.DoesNotContain(
            typeof(IntervalRetryPolicy).GetConstructors(),
            constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(int[])));
        Assert.DoesNotContain(
            typeof(Retry).GetMethods(),
            method => method.Name == nameof(Retry.Intervals)
                && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(int[])));

        Assert.DoesNotContain(
            typeof(Retry).GetMethods(),
            method => method.Name == nameof(Retry.Interval)
                && method.GetParameters() is [_, { ParameterType: var intervalType }]
                && intervalType == typeof(int));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "interval-budget-and-delays-are-exact")]
    public void PolicyContext_UsesEveryConfiguredDelayExactlyOnce()
    {
        TimeSpan[] expectedDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];
        IRetryPolicy policy = Retry.Intervals(expectedDelays);
        var failure = new RetryFailureException();
        using RetryPolicyContext<TestPipeContext> policyContext = policy.CreatePolicyContext(new TestPipeContext());

        Assert.True(policyContext.CanRetry(failure, out RetryContext<TestPipeContext> first));
        Assert.Equal(expectedDelays[0], first.Delay);
        Assert.True(first.CanRetry(failure, out RetryContext<TestPipeContext> second));
        Assert.Equal(expectedDelays[1], second.Delay);
        Assert.False(second.CanRetry(failure, out RetryContext<TestPipeContext> terminal));
        Assert.Equal(2, terminal.RetryCount);
        Assert.Null(terminal.Delay);
    }

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RetryFailureException : Exception;
}
