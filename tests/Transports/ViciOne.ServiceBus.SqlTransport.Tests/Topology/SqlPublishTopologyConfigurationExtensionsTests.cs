using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Topology;

public sealed class SqlPublishTopologyConfigurationExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-PUBLISH-SCAN", "namespace-scan-filters-valid-contracts")]
    public void NamespaceScan_PublishesOnlyValidFilteredContractsAndInvokesCallbacks()
    {
        ISqlBusFactoryConfigurator configurator = CreateConfigurator(out RecordingConfiguratorProxy recorder);
        Type[] selected = [typeof(IScanMessage), typeof(ScanMessage), typeof(InvalidScanMessage), typeof(OpenScanMessage<>)];
        var configured = new List<Type>();

        configurator.AddPublishMessageTypesFromNamespaceContaining(
            typeof(ScanMessage),
            (_, type) => configured.Add(type),
            selected.Contains);

        Assert.Equal([typeof(IScanMessage), typeof(ScanMessage)], recorder.Published.OrderBy(type => type.Name));
        Assert.Equal(recorder.Published.OrderBy(type => type.Name), configured.OrderBy(type => type.Name));
        Assert.DoesNotContain(typeof(InvalidScanMessage), recorder.Published);
        Assert.DoesNotContain(typeof(OpenScanMessage<>), recorder.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-PUBLISH-SCAN", "default-scan-rejects-invalid-contracts")]
    public void NamespaceScan_WithoutFilterStillRejectsInvalidContracts()
    {
        ISqlBusFactoryConfigurator configurator = CreateConfigurator(out RecordingConfiguratorProxy recorder);

        configurator.AddPublishMessageTypesFromNamespaceContaining(typeof(ScanMessage));

        Assert.Contains(typeof(IScanMessage), recorder.Published);
        Assert.Contains(typeof(ScanMessage), recorder.Published);
        Assert.DoesNotContain(typeof(InvalidScanMessage), recorder.Published);
        Assert.DoesNotContain(typeof(OpenScanMessage<>), recorder.Published);
        Assert.DoesNotContain(typeof(Address.SqlAddressTests), recorder.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-PUBLISH-SCAN", "explicit-types-preserve-order-and-callback-identity")]
    public void ExplicitTypes_PreserveOrderAndCallbackIdentity()
    {
        ISqlBusFactoryConfigurator configurator = CreateConfigurator(out RecordingConfiguratorProxy recorder);
        Type[] types = [typeof(ScanMessage), typeof(IScanMessage)];
        var configured = new List<Type>();

        configurator.AddPublishMessageTypes(types, (_, type) => configured.Add(type));

        Assert.Equal(types, recorder.Published);
        Assert.Equal(types, configured);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-PUBLISH-SCAN", "required-inputs-fail-at-public-boundary")]
    public void Registration_RejectsEveryMissingRequiredInput()
    {
        ISqlBusFactoryConfigurator configurator = CreateConfigurator(out RecordingConfiguratorProxy recorder);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SqlPublishTopologyConfigurationExtensions.AddPublishMessageTypesFromNamespaceContaining<ScanMessage>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SqlPublishTopologyConfigurationExtensions.AddPublishMessageTypesFromNamespaceContaining(null!, typeof(ScanMessage))).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            configurator.AddPublishMessageTypesFromNamespaceContaining(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SqlPublishTopologyConfigurationExtensions.AddPublishMessageTypes(null!, [typeof(ScanMessage)])).ParamName);
        Assert.Equal("messageTypes", Assert.Throws<ArgumentNullException>(() =>
            configurator.AddPublishMessageTypes(null!)).ParamName);
        Assert.Equal("messageTypes", Assert.Throws<ArgumentException>(() =>
            configurator.AddPublishMessageTypes([typeof(ScanMessage), null!])).ParamName);
        Assert.Empty(recorder.Published);
    }

    private static ISqlBusFactoryConfigurator CreateConfigurator(out RecordingConfiguratorProxy recorder)
    {
        ISqlBusFactoryConfigurator configurator =
            DispatchProxy.Create<ISqlBusFactoryConfigurator, RecordingConfiguratorProxy>();
        recorder = (RecordingConfiguratorProxy)(object)configurator;
        return configurator;
    }

    private class RecordingConfiguratorProxy : DispatchProxy
    {
        public List<Type> Published { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ISqlBusFactoryConfigurator.Publish) && args?[0] is Type messageType)
            {
                Published.Add(messageType);
                ISqlMessagePublishTopologyConfigurator topology =
                    DispatchProxy.Create<ISqlMessagePublishTopologyConfigurator, PassiveProxy>();
                (args[1] as Delegate)?.DynamicInvoke(topology);
            }

            return targetMethod?.ReturnType != typeof(void) && targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType != typeof(void) && targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }

    public interface IScanMessage;
    public sealed record ScanMessage;
    public delegate void InvalidScanMessage();
    public sealed record OpenScanMessage<T>;
}
