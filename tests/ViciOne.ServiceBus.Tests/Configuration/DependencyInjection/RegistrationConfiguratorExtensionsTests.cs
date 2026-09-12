using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

public sealed class RegistrationConfiguratorExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT-CONFIGURATION", "explicit-default-invariant")]
    public void DefaultRequestTimeout_AcceptsOnlyAnExplicitPositiveDuration()
    {
        var configurator = new InspectableServiceCollectionBusConfigurator(new ServiceCollection());
        var expected = new RequestTimeout(TimeSpan.FromSeconds(41));

        configurator.SetDefaultRequestTimeout(expected);
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configurator.SetDefaultRequestTimeout(RequestTimeout.None));

        Assert.Equal(expected, configurator.ConfiguredDefaultRequestTimeout);
        Assert.Equal("timeout", exception.ParamName);
        Assert.Equal("The default request timeout must be explicit. (Parameter 'timeout')", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT-CONFIGURATION", "no-unit-ambiguous-components")]
    public void AdvancedRegistration_DoesNotExposeUnitAmbiguousTimeoutComponents()
    {
        Assert.DoesNotContain(
            typeof(Advanced.Registration.IAdvancedRegistrationConfigurator).GetMethods(),
            method => method.Name == "SetDefaultRequestTimeout");
    }

    private sealed class InspectableServiceCollectionBusConfigurator(IServiceCollection services)
        : ServiceCollectionBusConfigurator(services)
    {
        public RequestTimeout ConfiguredDefaultRequestTimeout => DefaultRequestTimeout;
    }
}
