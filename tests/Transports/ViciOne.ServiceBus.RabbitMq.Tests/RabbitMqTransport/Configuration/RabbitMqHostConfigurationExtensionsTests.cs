using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class RabbitMqHostConfigurationExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-EXTENSIONS", "absolute-uri-and-plain-host-resolution")]
    public void HostString_DistinguishesAbsoluteAddressesFromPlainHostNames()
    {
        IRabbitMqBusFactoryConfigurator configurator = CreateRecorder(out RecordingConfiguratorProxy recorder);

        configurator.Host("rabbitmq://broker:5678/production");
        configurator.Host("plain-host");

        Assert.Collection(
            recorder.Settings,
            uri =>
            {
                Assert.Equal("broker", uri.Host);
                Assert.Equal(5678, uri.Port);
                Assert.Equal("production", uri.VirtualHost);
            },
            plain =>
            {
                Assert.Equal("plain-host", plain.Host);
                Assert.Equal(5672, plain.Port);
                Assert.Equal("/", plain.VirtualHost);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-EXTENSIONS", "explicit-port-name-and-callback")]
    public void ExplicitHost_AppliesPortConnectionNameAndCallback()
    {
        IRabbitMqBusFactoryConfigurator configurator = CreateRecorder(out RecordingConfiguratorProxy recorder);

        configurator.Host("broker", 5679, "production", "client-a", host => host.Username("service-user"));

        RabbitMqHostSettings settings = Assert.Single(recorder.Settings);
        Assert.Equal("broker", settings.Host);
        Assert.Equal(5679, settings.Port);
        Assert.Equal("production", settings.VirtualHost);
        Assert.Equal("client-a", settings.ClientProvidedName);
        Assert.Equal("service-user", settings.Username);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-EXTENSIONS", "required-host-inputs")]
    public void ExplicitHost_RejectsNullHostAndVirtualHost()
    {
        IRabbitMqBusFactoryConfigurator configurator = CreateRecorder(out _);

        Assert.Equal("host", Assert.Throws<ArgumentNullException>(() =>
            RabbitMqHostConfigurationExtensions.Host(configurator, (string)null!, "/")).ParamName);
        Assert.Equal("virtualHost", Assert.Throws<ArgumentNullException>(() =>
            RabbitMqHostConfigurationExtensions.Host(configurator, "broker", (string)null!)).ParamName);
        Assert.Equal("host", Assert.Throws<ArgumentNullException>(() =>
            RabbitMqHostConfigurationExtensions.Host(configurator, null!, 5672, "/", connectionName: null, configure: null)).ParamName);
        Assert.Equal("virtualHost", Assert.Throws<ArgumentNullException>(() =>
            RabbitMqHostConfigurationExtensions.Host(configurator, "broker", 5672, null!, connectionName: null, configure: null)).ParamName);
    }

    private static IRabbitMqBusFactoryConfigurator CreateRecorder(out RecordingConfiguratorProxy recorder)
    {
        IRabbitMqBusFactoryConfigurator configurator = DispatchProxy.Create<IRabbitMqBusFactoryConfigurator, RecordingConfiguratorProxy>();
        recorder = (RecordingConfiguratorProxy)(object)configurator;
        return configurator;
    }

    private class RecordingConfiguratorProxy : DispatchProxy
    {
        public List<RabbitMqHostSettings> Settings { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IRabbitMqBusFactoryConfigurator.Host) && args?[0] is RabbitMqHostSettings settings)
            {
                Settings.Add(settings);
                return null;
            }

            return targetMethod?.ReturnType.IsValueType == true ? Activator.CreateInstance(targetMethod.ReturnType) : null;
        }
    }
}
