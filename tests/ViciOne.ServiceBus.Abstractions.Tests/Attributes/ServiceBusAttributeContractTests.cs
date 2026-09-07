using System.Reflection;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Attributes;

public sealed class ServiceBusAttributeContractTests
{
    [Theory]
    [InlineData(typeof(ConfigureConsumeTopologyAttribute), AttributeTargets.Class | AttributeTargets.Interface)]
    [InlineData(typeof(EntityNameAttribute), AttributeTargets.Class | AttributeTargets.Interface)]
    [InlineData(typeof(ExcludeFromConfigureEndpointsAttribute), AttributeTargets.Class | AttributeTargets.Interface)]
    [InlineData(typeof(ExcludeFromImplementedTypesAttribute), AttributeTargets.Class | AttributeTargets.Interface)]
    [InlineData(typeof(ExcludeFromTopologyAttribute), AttributeTargets.Class | AttributeTargets.Interface)]
    [InlineData(typeof(FaultEntityNameAttribute), AttributeTargets.Class | AttributeTargets.Interface)]
    [InlineData(typeof(IndexedAttribute), AttributeTargets.Property)]
    [InlineData(typeof(MessageUrnAttribute), AttributeTargets.Class | AttributeTargets.Interface)]
    [RequirementCoverage("REQ-VSB-ATTRIBUTE-CONTRACTS", "sealed-single-use-exact-targets")]
    public void AttributeContracts_AreSealedAndDeclareExactTargets(Type attributeType, AttributeTargets expectedTargets)
    {
        Assert.True(attributeType.IsSealed, $"{attributeType.FullName} must be sealed.");
        AttributeUsageAttribute usage = Assert.IsType<AttributeUsageAttribute>(
            attributeType.GetCustomAttribute<AttributeUsageAttribute>());
        Assert.Equal(expectedTargets, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.True(usage.Inherited);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TOPOLOGY-ATTRIBUTE", "default-and-explicit-decisions")]
    public void ConfigureConsumeTopology_PreservesDefaultAndExplicitDecisions()
    {
        Assert.True(new ConfigureConsumeTopologyAttribute().ConfigureConsumeTopology);
        Assert.True(new ConfigureConsumeTopologyAttribute(true).ConfigureConsumeTopology);
        Assert.False(new ConfigureConsumeTopologyAttribute(false).ConfigureConsumeTopology);
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "")]
    [InlineData(false, " ")]
    [InlineData(true, null)]
    [InlineData(true, "")]
    [InlineData(true, " ")]
    [RequirementCoverage("REQ-VSB-ENTITY-NAME-ATTRIBUTES", "missing-names-rejected")]
    public void EntityNameAttributes_RejectMissingNames(bool faultName, string? entityName)
    {
        ArgumentException exception = faultName
            ? Assert.ThrowsAny<ArgumentException>(() => new FaultEntityNameAttribute(entityName!))
            : Assert.ThrowsAny<ArgumentException>(() => new EntityNameAttribute(entityName!));

        Assert.Equal("entityName", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENTITY-NAME-ATTRIBUTES", "declared-names-preserved")]
    public void EntityNameAttributes_PreserveDeclaredNames()
    {
        Assert.Equal("orders", new EntityNameAttribute("orders").EntityName);
        Assert.Equal("order-faults", new FaultEntityNameAttribute("order-faults").EntityName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENTITY-NAME-ATTRIBUTES", "message-and-fault-formatting-overrides")]
    public void EntityNameFormatter_UsesMessageAndFaultOverrides()
    {
        var fallback = new CountingEntityNameFormatter();

        string messageName = new MessageEntityNameFormatter<EntityNamedMessage>(fallback).FormatEntityName();
        string faultName = new MessageEntityNameFormatter<Fault<FaultNamedMessage>>(fallback).FormatEntityName();

        Assert.Equal("orders", messageName);
        Assert.Equal("order-faults", faultName);
        Assert.Equal(0, fallback.CallCount);
    }

    [EntityName("orders")]
    private sealed class EntityNamedMessage
    {
    }

    [FaultEntityName("order-faults")]
    private sealed class FaultNamedMessage
    {
    }

    private sealed class CountingEntityNameFormatter : IEntityNameFormatter
    {
        public int CallCount { get; private set; }

        public string FormatEntityName<T>()
        {
            CallCount++;
            return typeof(T).Name;
        }
    }
}
