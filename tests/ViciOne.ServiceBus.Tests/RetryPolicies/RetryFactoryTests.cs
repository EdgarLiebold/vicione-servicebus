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
}
