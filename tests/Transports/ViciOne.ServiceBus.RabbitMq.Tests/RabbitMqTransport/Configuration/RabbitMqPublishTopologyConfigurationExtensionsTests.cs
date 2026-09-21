using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class RabbitMqPublishTopologyConfigurationExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-PUBLISH-SCAN", "namespace-scan-filters-valid-message-contracts")]
    public void NamespaceScan_PublishesOnlyValidFilteredContractsAndInvokesTheirCallbacks()
    {
        IRabbitMqBusFactoryConfigurator configurator = DispatchProxy.Create<IRabbitMqBusFactoryConfigurator, RecordingConfiguratorProxy>();
        var recorder = (RecordingConfiguratorProxy)(object)configurator;
        Type[] selected = [typeof(IPublishScanMessage), typeof(PublishScanMessage), typeof(PublishScanGenericMessage<>)];
        var configured = new List<Type>();

        configurator.AddPublishMessageTypesFromNamespaceContaining(
            typeof(PublishScanMessage),
            (_, type) => configured.Add(type),
            type => selected.Contains(type));

        Assert.Equal(
            [typeof(IPublishScanMessage), typeof(PublishScanMessage)],
            recorder.Published.OrderBy(type => type.Name));
        Assert.Equal(recorder.Published.OrderBy(type => type.Name), configured.OrderBy(type => type.Name));
        Assert.DoesNotContain(typeof(PublishScanGenericMessage<>), recorder.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-PUBLISH-SCAN", "explicit-types-preserve-order-and-callback-identity")]
    public void ExplicitTypes_PreserveInputOrderAndCallbackIdentity()
    {
        IRabbitMqBusFactoryConfigurator configurator = DispatchProxy.Create<IRabbitMqBusFactoryConfigurator, RecordingConfiguratorProxy>();
        var recorder = (RecordingConfiguratorProxy)(object)configurator;
        Type[] types = [typeof(PublishScanMessage), typeof(IPublishScanMessage)];
        var configured = new List<Type>();

        configurator.AddPublishMessageTypes(types, (_, type) => configured.Add(type));

        Assert.Equal(types, recorder.Published);
        Assert.Equal(types, configured);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-PUBLISH-SCAN", "required-input-validation")]
    public void NamespaceScan_RejectsMissingConfiguratorAndMarkerType()
    {
        IRabbitMqBusFactoryConfigurator configurator = DispatchProxy.Create<IRabbitMqBusFactoryConfigurator, RecordingConfiguratorProxy>();

        Assert.Equal(
            "configurator",
            Assert.Throws<ArgumentNullException>(
                () => RabbitMqPublishTopologyConfigurationExtensions.AddPublishMessageTypesFromNamespaceContaining(
                    null!, typeof(PublishScanMessage))).ParamName);
        Assert.Equal(
            "type",
            Assert.Throws<ArgumentNullException>(
                () => configurator.AddPublishMessageTypesFromNamespaceContaining(null!)).ParamName);
    }

    private class RecordingConfiguratorProxy : DispatchProxy
    {
        public List<Type> Published { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IRabbitMqBusFactoryConfigurator.Publish) && args?[0] is Type messageType)
            {
                Published.Add(messageType);
                IRabbitMqMessagePublishTopologyConfigurator topology =
                    DispatchProxy.Create<IRabbitMqMessagePublishTopologyConfigurator, PassiveProxy>();
                (args[1] as Delegate)?.DynamicInvoke(topology);
                return null;
            }

            return Default(targetMethod?.ReturnType);
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Default(targetMethod?.ReturnType);
    }

    private static object? Default(Type? type) => type?.IsValueType == true ? Activator.CreateInstance(type) : null;
}
