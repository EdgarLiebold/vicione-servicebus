using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class RetryFactoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY-FACTORY", "policy-only-configuration-surface")]
    public void CreatePolicy_ExposesPolicyConfigurationWithoutAnInoperativeObserverSurface()
    {
        Assert.False(typeof(IRetryObserverConnector).IsAssignableFrom(typeof(IRetryPolicyConfigurator)));

        IRetryPolicy policy = Retry.CreatePolicy(configurator =>
            configurator.Immediate(2).Handle<InvalidOperationException>());

        ImmediateRetryPolicy immediate = Assert.IsType<ImmediateRetryPolicy>(policy);
        Assert.Equal(2, immediate.RetryLimit);
        Assert.True(immediate.IsHandled(new InvalidOperationException()));
        Assert.False(immediate.IsHandled(new ArgumentException()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY-FACTORY", "configuration-callback-is-required")]
    public void CreatePolicy_RejectsANullConfigurationCallback()
    {
        Assert.Throws<ArgumentNullException>(() => Retry.CreatePolicy(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY-FACTORY", "policy-factory-is-required")]
    public void CreatePolicy_RejectsMissingAndNullPolicyFactories()
    {
        ConfigurationException missing = Assert.Throws<ConfigurationException>(() => Retry.CreatePolicy(_ => { }));

        Assert.Contains("RetryPolicy", missing.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => Retry.CreatePolicy(configurator => configurator.SetRetryPolicy(null!)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY-FACTORY", "factory-must-produce-policy")]
    public void CreatePolicy_WrapsAFactoryThatReturnsNull()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            Retry.CreatePolicy(configurator => configurator.SetRetryPolicy(_ => null!)));

        InvalidOperationException cause = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("returned null", cause.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY-FACTORY", "every-direct-policy-overload-preserves-schedule-and-filter")]
    public void DirectPolicyFactories_PreserveEveryScheduleAndFilterOverload()
    {
        TimeSpan first = TimeSpan.FromMilliseconds(11);
        TimeSpan second = TimeSpan.FromMilliseconds(23);
        IExceptionFilter selected = Retry.Selected<SelectedFailure>();

        Assert.Equal(2, Assert.IsType<ImmediateRetryPolicy>(Retry.Immediate(2)).RetryLimit);
        ImmediateRetryPolicy filteredImmediate = Assert.IsType<ImmediateRetryPolicy>(selected.Immediate(3));
        Assert.Equal(3, filteredImmediate.RetryLimit);
        Assert.True(filteredImmediate.IsHandled(new SelectedFailure()));
        Assert.False(filteredImmediate.IsHandled(new IgnoredFailure()));

        Assert.Equal([first, second], Assert.IsType<IntervalRetryPolicy>(Retry.Intervals(first, second)).Intervals);
        IntervalRetryPolicy filteredIntervals = Assert.IsType<IntervalRetryPolicy>(selected.Intervals(second, first));
        Assert.Equal([second, first], filteredIntervals.Intervals);
        Assert.True(filteredIntervals.IsHandled(new SelectedFailure()));
        Assert.Equal([first, first, first], Assert.IsType<IntervalRetryPolicy>(Retry.Interval(3, first)).Intervals);
        Assert.Equal([second, second], Assert.IsType<IntervalRetryPolicy>(selected.Interval(2, second)).Intervals);

        ExponentialRetryPolicy bounded = Assert.IsType<ExponentialRetryPolicy>(
            Retry.Exponential(4, first, TimeSpan.FromSeconds(1), second));
        ExponentialRetryPolicy continuing = Assert.IsType<ExponentialRetryPolicy>(
            Retry.Exponential(first, TimeSpan.FromSeconds(1), second));
        ExponentialRetryPolicy filteredExponential = Assert.IsType<ExponentialRetryPolicy>(
            selected.Exponential(5, first, TimeSpan.FromSeconds(1), second));
        Assert.Equal(4, bounded.RetryLimit);
        Assert.Equal(int.MaxValue, continuing.RetryLimit);
        Assert.Equal(5, filteredExponential.RetryLimit);
        Assert.True(filteredExponential.IsHandled(new SelectedFailure()));
        Assert.False(filteredExponential.IsHandled(new IgnoredFailure()));

        IncrementalRetryPolicy incremental = Assert.IsType<IncrementalRetryPolicy>(Retry.Incremental(6, first, second));
        IncrementalRetryPolicy filteredIncremental = Assert.IsType<IncrementalRetryPolicy>(selected.Incremental(7, second, first));
        Assert.Equal((6, first, second), (incremental.RetryLimit, incremental.InitialInterval, incremental.IntervalIncrement));
        Assert.Equal((7, second, first),
            (filteredIncremental.RetryLimit, filteredIncremental.InitialInterval, filteredIncremental.IntervalIncrement));
        Assert.True(filteredIncremental.IsHandled(new SelectedFailure()));
        Assert.False(filteredIncremental.IsHandled(new IgnoredFailure()));
        Assert.Same(Retry.All(), Retry.All());
        Assert.IsType<NoRetryPolicy>(Retry.None);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-FILTER", "every-generic-arity-and-runtime-type-form-has-exact-membership")]
    public void ExceptionFilterFactories_PreserveEveryGenericArityAndRuntimeTypeForm()
    {
        IExceptionFilter[] selected =
        [
            Retry.Selected(typeof(SelectedFailure), typeof(SecondFailure), typeof(ThirdFailure)),
            Retry.Selected<SelectedFailure>(),
            Retry.Selected<SelectedFailure, SecondFailure>(),
            Retry.Selected<SelectedFailure, SecondFailure, ThirdFailure>(),
        ];
        IExceptionFilter[] except =
        [
            Retry.Except(typeof(SelectedFailure), typeof(SecondFailure), typeof(ThirdFailure)),
            Retry.Except<SelectedFailure>(),
            Retry.Except<SelectedFailure, SecondFailure>(),
            Retry.Except<SelectedFailure, SecondFailure, ThirdFailure>(),
        ];

        Assert.All(selected, filter =>
        {
            Assert.True(filter.Match(new SelectedFailure()));
            Assert.False(filter.Match(new IgnoredFailure()));
        });
        Assert.All(except, filter =>
        {
            Assert.False(filter.Match(new SelectedFailure()));
            Assert.True(filter.Match(new IgnoredFailure()));
        });

        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() => Retry.Immediate(null!, 1)).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() => Retry.Intervals(null!, TimeSpan.Zero)).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() => Retry.Interval(null!, 1, TimeSpan.Zero)).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
            Retry.Exponential(null!, 1, TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1))).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
            Retry.Incremental(null!, 1, TimeSpan.Zero, TimeSpan.Zero)).ParamName);
    }

    private sealed class SelectedFailure : Exception;

    private sealed class SecondFailure : Exception;

    private sealed class ThirdFailure : Exception;

    private sealed class IgnoredFailure : Exception;
}
