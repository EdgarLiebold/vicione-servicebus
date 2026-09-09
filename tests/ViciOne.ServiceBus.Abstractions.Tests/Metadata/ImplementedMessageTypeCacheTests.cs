using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Metadata;

public sealed class ImplementedMessageTypeCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-IMPLEMENTED-MESSAGE-TYPE-CACHE", "interface-chain")]
    public void InterfaceChain_ContainsOnlyTheImmediateParent()
    {
        AssertTopology<LeafContract>(typeof(IntermediateContract));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-IMPLEMENTED-MESSAGE-TYPE-CACHE", "interface-diamond")]
    public void InterfaceDiamond_ContainsEachImmediateParentOnce()
    {
        AssertTopology<DiamondContract>(typeof(LeftContract), typeof(RightContract));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-IMPLEMENTED-MESSAGE-TYPE-CACHE", "class-base-and-interface")]
    public void ClassHierarchy_ContainsTheImmediateBaseAndItsMostSpecificInterface()
    {
        AssertTopology<DerivedMessage>(typeof(ExcludedMiddleMessage), typeof(RootContract));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-IMPLEMENTED-MESSAGE-TYPE-CACHE", "fault-polymorphism")]
    public void Fault_ContainsTheNonGenericFaultAndEveryDirectPolymorphicFault()
    {
        AssertTopology<Fault<DerivedMessage>>(
            typeof(Fault),
            typeof(Fault<ExcludedMiddleMessage>),
            typeof(Fault<RootContract>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-IMPLEMENTED-MESSAGE-TYPE-CACHE", "invalid-system-interface")]
    public void SystemInterface_IsNotAMessageTopologyParent()
    {
        AssertTopology<SystemInterfaceMessage>(typeof(RootContract));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-IMPLEMENTED-MESSAGE-TYPE-CACHE", "no-parent")]
    public void StandaloneMessage_HasNoImplementedTopologyParent()
    {
        AssertTopology<StandaloneMessage>();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-IMPLEMENTED-MESSAGE-TYPE-CACHE", "required-enumerator")]
    public void Enumeration_RejectsAMissingConsumerForEveryTopologyShape()
    {
        Assert.Equal("implementedMessageType", Assert.Throws<ArgumentNullException>(
            () => ImplementedMessageTypeCache<LeafContract>.EnumerateImplementedTypes(null!)).ParamName);
        Assert.Equal("implementedMessageType", Assert.Throws<ArgumentNullException>(
            () => ImplementedMessageTypeCache<StandaloneMessage>.EnumerateImplementedTypes(null!)).ParamName);
    }

    private static void AssertTopology<TMessage>(params Type[] expectedTypes)
        where TMessage : class
    {
        var collector = new ImplementedTypeCollector();

        ImplementedMessageTypeCache<TMessage>.EnumerateImplementedTypes(collector);

        var actualTypes = collector.Entries
            .Select(entry => entry.MessageType)
            .OrderBy(GetStableTypeName, StringComparer.Ordinal)
            .ToArray();
        var sortedExpectedTypes = expectedTypes
            .OrderBy(GetStableTypeName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(sortedExpectedTypes, actualTypes);
        Assert.Equal(actualTypes.Length, actualTypes.Distinct().Count());
        Assert.All(collector.Entries, entry => Assert.True(entry.Direct));
    }

    private static string GetStableTypeName(Type type) =>
        type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

    private sealed class ImplementedTypeCollector : IImplementedMessageType
    {
        public List<ImplementedTypeEntry> Entries { get; } = [];

        public void ImplementsMessageType<T>(bool direct)
            where T : class
        {
            Entries.Add(new ImplementedTypeEntry(typeof(T), direct));
        }
    }

    private readonly record struct ImplementedTypeEntry(Type MessageType, bool Direct);

    private interface RootContract
    {
    }

    private interface IntermediateContract : RootContract
    {
    }

    private interface LeafContract : IntermediateContract
    {
    }

    private interface LeftContract : RootContract
    {
    }

    private interface RightContract : RootContract
    {
    }

    private interface DiamondContract : LeftContract, RightContract
    {
    }

    private abstract class RootMessage
    {
    }

    [ExcludeFromTopology]
    private abstract class ExcludedMiddleMessage : RootMessage, RootContract
    {
    }

    private sealed class DerivedMessage : ExcludedMiddleMessage
    {
    }

    private sealed class SystemInterfaceMessage : RootContract, IComparable<SystemInterfaceMessage>
    {
        public int CompareTo(SystemInterfaceMessage? other) => 0;
    }

    private sealed class StandaloneMessage
    {
    }
}
