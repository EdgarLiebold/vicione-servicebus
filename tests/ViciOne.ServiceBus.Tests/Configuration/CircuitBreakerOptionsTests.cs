using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class CircuitBreakerOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "safe-and-explicit-defaults")]
    public void Defaults_AreBoundedExplicitAndUseStandardTime()
    {
        var options = new CircuitBreakerOptions();

        Assert.Equal(5, options.MinimumThroughput);
        Assert.Equal(0.05, options.FailureRatio);
        Assert.Equal(TimeSpan.FromMinutes(1), options.SamplingDuration);
        Assert.Equal(
            [100, 200, 500, 1_000, 5_000, 10_000, 15_000, 30_000, 60_000],
            options.BreakDurations.Select(duration => duration.TotalMilliseconds));
        Assert.Same(TimeProvider.System, options.TimeProvider);
        Assert.True(options.ExceptionFilter.Match(new InvalidOperationException("matching default")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "threshold-boundaries")]
    public void ThresholdConfiguration_AcceptsExactBoundsAndRejectsEveryInvalidValue()
    {
        var options = new CircuitBreakerOptions();

        Assert.Same(options, options.SetMinimumThroughput(1));
        Assert.Same(options, options.SetFailureRatio(0));
        Assert.Same(options, options.SetFailureRatio(1));
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMinimumThroughput(0)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetFailureRatio(-double.Epsilon)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetFailureRatio(1.01)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetFailureRatio(double.NaN)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetFailureRatio(double.PositiveInfinity)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "time-and-duration-boundaries")]
    public void TimeConfiguration_IsPositiveBoundedAndDefensivelyCopied()
    {
        var options = new CircuitBreakerOptions();
        TimeSpan[] durations = [TimeSpan.FromTicks(1), TimeSpan.FromSeconds(2)];

        Assert.Same(options, options.SetSamplingDuration(TimeSpan.FromTicks(1)));
        Assert.Same(options, options.SetBreakDurations(durations));
        durations[0] = TimeSpan.FromDays(1);

        Assert.Equal(TimeSpan.FromTicks(1), options.SamplingDuration);
        Assert.Equal([TimeSpan.FromTicks(1), TimeSpan.FromSeconds(2)], options.BreakDurations);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetSamplingDuration(TimeSpan.Zero)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetSamplingDuration(TimeSpan.FromTicks(-1))).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => options.SetBreakDurations(null!)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentException>(() => options.SetBreakDurations()).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetBreakDurations(TimeSpan.Zero)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetBreakDurations(TimeSpan.FromTicks(-1))).ParamName);
        Assert.Equal(
            "retryAfter",
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircuitBreakerOpenException(TimeSpan.FromTicks(-1), false, null)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "required-collaborators-and-filter")]
    public void Collaborators_AreRequiredAndTheExceptionFilterIsExplicit()
    {
        var options = new CircuitBreakerOptions()
            .SetExceptionFilter(filter => filter.Handle<InvalidOperationException>());

        Assert.True(options.ExceptionFilter.Match(new InvalidOperationException("included")));
        Assert.False(options.ExceptionFilter.Match(new ArgumentException("excluded")));
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => options.SetTimeProvider(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => options.SetExceptionFilter(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "retained-filter-builder-cannot-mutate-snapshot")]
    public void RetainedExceptionConfigurator_CannotMutateThePublishedFilterSnapshot()
    {
        IExceptionConfigurator retained = null!;
        var options = new CircuitBreakerOptions()
            .SetExceptionFilter(configurator =>
            {
                retained = configurator;
                configurator.Handle<ExpectedFailureException>();
            });

        retained.Ignore<ExpectedFailureException>();

        Assert.True(options.ExceptionFilter.Match(new ExpectedFailureException()));
        Assert.False(options.ExceptionFilter.Match(new ArgumentException("not configured")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "caller-owned-type-array-cannot-mutate-snapshot")]
    public void CallerOwnedExceptionTypeArray_CannotMutateThePublishedFilterSnapshot()
    {
        Type[] handledTypes = [typeof(ExpectedFailureException)];
        var options = new CircuitBreakerOptions()
            .SetExceptionFilter(configurator => configurator.Handle(handledTypes));

        handledTypes[0] = typeof(ArgumentException);

        Assert.True(options.ExceptionFilter.Match(new ExpectedFailureException()));
        Assert.False(options.ExceptionFilter.Match(new ArgumentException("not configured")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "immutable-runtime-snapshot")]
    public async Task BuiltPipe_UsesAnImmutableConfigurationSnapshot()
    {
        var initialTime = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var replacementTime = new FakeTimeProvider(new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions? captured = null;
        var entered = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseCircuitBreaker(options =>
            {
                captured = options;
                options
                    .SetMinimumThroughput(1)
                    .SetFailureRatio(0)
                    .SetBreakDuration(TimeSpan.FromSeconds(1))
                    .SetTimeProvider(initialTime);
            });
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref entered);
                throw new ExpectedFailureException();
            });
        });

        Assert.NotNull(captured);
        captured
            .SetMinimumThroughput(100)
            .SetBreakDuration(TimeSpan.FromDays(1))
            .SetTimeProvider(replacementTime);

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        CircuitBreakerOpenException rejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.Send(new TestPipeContext()));
        initialTime.Advance(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));

        Assert.Equal(2, entered);
        Assert.Equal(TimeSpan.FromSeconds(1), rejection.RetryAfter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-CONFIGURATION", "greenfield-public-surface")]
    public void PublicSurface_ExposesOptionsAndRejectionButNoRuntimeStateOrRouterEvents()
    {
        string[] fluentMethods = typeof(CircuitBreakerOptions)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] stalePublicTypes = typeof(CircuitBreakerOptions).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => name.Contains("ICircuitBreakerConfigurator", StringComparison.Ordinal)
                || name.Contains("CircuitBreakerClosed", StringComparison.Ordinal)
                || name.Contains("CircuitBreakerOpened", StringComparison.Ordinal)
                || name.Contains("CircuitBreakerBehavior", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(
            [
                "SetBreakDuration",
                "SetBreakDurations",
                "SetExceptionFilter",
                "SetFailureRatio",
                "SetMinimumThroughput",
                "SetSamplingDuration",
                "SetTimeProvider",
            ],
            fluentMethods);
        Assert.Empty(stalePublicTypes);
        Assert.True(typeof(CircuitBreakerOptions).IsSealed);
        Assert.True(typeof(CircuitBreakerOpenException).IsSealed);
        Assert.All(
            typeof(CircuitBreakerOptions).GetProperties(BindingFlags.Instance | BindingFlags.Public),
            property => Assert.False(property.SetMethod?.IsPublic ?? false));
    }

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class ExpectedFailureException : Exception;
}
