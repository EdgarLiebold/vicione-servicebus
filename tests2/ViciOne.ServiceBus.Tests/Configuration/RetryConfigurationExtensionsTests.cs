using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class RetryConfigurationExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "configuration-boundary-rejects-null-inputs")]
    public void UseRetry_RejectsANullConfiguratorAndConfigurationDelegate()
    {
        ArgumentNullException configuratorException = Assert.Throws<ArgumentNullException>(() =>
            RetryConfigurationExtensions.UseRetry<TestPipeContext>(null!, _ => { }));

        var configurator = new PipeConfigurator<TestPipeContext>();

        ArgumentNullException delegateException = Assert.Throws<ArgumentNullException>(() =>
            configurator.UseRetry(null!));

        Assert.Equal("configurator", configuratorException.ParamName);
        Assert.Equal("configure", delegateException.ParamName);
    }

    private sealed class TestPipeContext : BasePipeContext
    {
    }
}
