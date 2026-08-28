using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests.Configuration;

public sealed class ActiveMqConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "no-implicit-host-or-credentials")]
    public void TransportOptions_DoNotInventAHostOrCredentials()
    {
        var options = new ActiveMqTransportOptions();

        Assert.Null(options.Host);
        Assert.Null(options.User);
        Assert.Null(options.Pass);
        Assert.Equal((ushort)61616, options.Port);
        Assert.False(options.UseSsl);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "host-must-be-explicit")]
    public void BusConfiguration_RejectsAnUnconfiguredHost()
    {
        ActiveMqBusFactoryConfigurator configurator = CreateConfigurator();

        ValidationResult failure = Assert.Single(
            configurator.Validate(),
            result => result.Disposition == ValidationResultDisposition.Failure
                && result.Key.Contains("Host", StringComparison.Ordinal));

        Assert.Contains("explicitly", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "explicit-host-satisfies-validation")]
    public void BusConfiguration_AcceptsAnExplicitHost()
    {
        ActiveMqBusFactoryConfigurator configurator = CreateConfigurator();
        configurator.Host("broker.internal", 61616, _ => { });

        Assert.DoesNotContain(
            configurator.Validate(),
            result => result.Disposition == ValidationResultDisposition.Failure
                && result.Key.Contains("Host", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "dead-connection-hook-removed")]
    public void ReceiveEndpointApi_ExposesOnlyTheEffectiveSessionHook()
    {
        string[] methodNames = typeof(IActiveMqReceiveEndpointConfigurator)
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();

        Assert.Contains(nameof(IActiveMqReceiveEndpointConfigurator.ConfigureSession), methodNames);
        Assert.DoesNotContain("ConfigureConnection", methodNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "product-test-harness-removed")]
    public void ProductAssembly_DoesNotShipTheLegacyActiveMqTestHarness()
    {
        Type? harness = typeof(ActiveMqTransportOptions).Assembly.GetType(
            "ViciOne.ServiceBus.Testing.ActiveMqTestHarness",
            throwOnError: false,
            ignoreCase: false);

        Assert.Null(harness);
    }

    private static ActiveMqBusFactoryConfigurator CreateConfigurator()
    {
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var busConfiguration = new ActiveMqBusConfiguration(topology);
        return new ActiveMqBusFactoryConfigurator(busConfiguration);
    }
}
