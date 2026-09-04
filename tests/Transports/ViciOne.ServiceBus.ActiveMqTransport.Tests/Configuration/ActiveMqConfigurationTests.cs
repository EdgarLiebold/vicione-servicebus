using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests.Configuration;

public sealed class ActiveMqConfigurationTests
{
    [Theory]
    [InlineData(ActiveMqTransportProtocol.OpenWire, 61616, "activemq")]
    [InlineData(ActiveMqTransportProtocol.Amqp, 5672, "amqp")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "dependency-injection-projects-selected-provider")]
    public async Task RegisteredOptions_ProjectTheSelectedProtocolIntoTheBusAsync(
        ActiveMqTransportProtocol protocol,
        ushort port,
        string expectedScheme)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddOptions<ActiveMqTransportOptions>().Configure(options =>
        {
            options.Host = "broker.internal";
            options.Protocol = protocol;
            options.Port = port;
        });
        services.AddViciOneServiceBus(configurator => configurator.UsingActiveMq());
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        IBus bus = provider.GetRequiredService<IBus>();

        Assert.Equal(expectedScheme, bus.Address.Scheme);
        Assert.Equal("broker.internal", bus.Address.Host);
        Assert.Equal(port, bus.Address.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "no-implicit-host-or-credentials")]
    public void TransportOptions_DoNotInventAHostOrCredentials()
    {
        var options = new ActiveMqTransportOptions();

        Assert.Null(options.Host);
        Assert.Null(options.Protocol);
        Assert.Null(options.Port);
        Assert.Null(options.User);
        Assert.Null(options.Pass);
        Assert.False(options.UseSsl);
    }

    [Theory]
    [InlineData(ActiveMqTransportProtocol.OpenWire, 61616, "activemq://broker.internal:61616/")]
    [InlineData(ActiveMqTransportProtocol.Amqp, 5672, "amqp://broker.internal:5672/")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "options-project-explicit-protocol-and-port")]
    public void TransportOptions_ProjectTheSelectedProtocolAndPort(
        ActiveMqTransportProtocol protocol,
        ushort port,
        string expected)
    {
        var options = new ActiveMqTransportOptions
        {
            Host = "broker.internal",
            Protocol = protocol,
            Port = port,
        };

        Uri address = Assert.IsType<Uri>(ActiveMqRegistrationBusFactory.GetHostAddress(options));

        Assert.Equal(new Uri(expected), address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "options-require-explicit-protocol")]
    public void TransportOptions_RejectAHostWithoutAProtocol()
    {
        var options = new ActiveMqTransportOptions { Host = "broker.internal", Port = 61616 };

        ActiveMqTransportConfigurationException exception = Assert.Throws<ActiveMqTransportConfigurationException>(
            () => ActiveMqRegistrationBusFactory.GetHostAddress(options));

        Assert.Contains("protocol", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData((ushort)0)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "options-require-explicit-port")]
    public void TransportOptions_RejectAMissingOrZeroPort(ushort? port)
    {
        var options = new ActiveMqTransportOptions
        {
            Host = "broker.internal",
            Protocol = ActiveMqTransportProtocol.Amqp,
            Port = port,
        };

        ActiveMqTransportConfigurationException exception = Assert.Throws<ActiveMqTransportConfigurationException>(
            () => ActiveMqRegistrationBusFactory.GetHostAddress(options));

        Assert.Contains("port", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "options-require-explicit-host")]
    public void TransportOptions_RejectAProtocolAndPortWithoutAHost()
    {
        var options = new ActiveMqTransportOptions
        {
            Protocol = ActiveMqTransportProtocol.Amqp,
            Port = 5672,
        };

        ActiveMqTransportConfigurationException exception = Assert.Throws<ActiveMqTransportConfigurationException>(
            () => ActiveMqRegistrationBusFactory.GetHostAddress(options));

        Assert.Contains("host", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "options-reject-unknown-protocol")]
    public void TransportOptions_RejectAnUnknownProtocolValue()
    {
        var options = new ActiveMqTransportOptions
        {
            Host = "broker.internal",
            Protocol = (ActiveMqTransportProtocol)999,
            Port = 5672,
        };

        ActiveMqTransportConfigurationException exception = Assert.Throws<ActiveMqTransportConfigurationException>(
            () => ActiveMqRegistrationBusFactory.GetHostAddress(options));

        Assert.Contains("not supported", exception.Message, StringComparison.OrdinalIgnoreCase);
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

    [Theory]
    [InlineData(ActiveMqTransportProtocol.OpenWire, 61616, "activemq")]
    [InlineData(ActiveMqTransportProtocol.Amqp, 5672, "amqp")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "explicit-host-satisfies-validation")]
    public void BusConfiguration_AcceptsAnExplicitProtocolHostAndPort(
        ActiveMqTransportProtocol protocol,
        int port,
        string expectedScheme)
    {
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var busConfiguration = new ActiveMqBusConfiguration(topology);
        var configurator = new ActiveMqBusFactoryConfigurator(busConfiguration);
        configurator.Host("broker.internal", protocol, port, _ => { });

        Assert.DoesNotContain(
            configurator.Validate(),
            result => result.Disposition == ValidationResultDisposition.Failure
                && result.Key.Contains("Host", StringComparison.Ordinal));
        Assert.Equal(expectedScheme, busConfiguration.HostConfiguration.Settings.HostAddress.Scheme);
        Assert.Equal("broker.internal", busConfiguration.HostConfiguration.Settings.HostAddress.Host);
        Assert.Equal(port, busConfiguration.HostConfiguration.Settings.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-CONFIGURATION", "protocol-less-host-overload-is-not-public-api")]
    public void HostApi_ExposesNoProtocolLessHostAndPortOverload()
    {
        var protocolLess = typeof(ActiveMqHostConfigurationExtensions)
            .GetMethods()
            .Where(method => method.Name == nameof(ActiveMqHostConfigurationExtensions.Host))
            .Where(method => method.GetParameters() is var parameters
                && parameters.Length == 4
                && parameters[1].ParameterType == typeof(string)
                && parameters[2].ParameterType == typeof(int))
            .ToArray();

        Assert.Empty(protocolLess);
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
