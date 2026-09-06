using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Metadata;

public sealed class FaultMessageTypeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-MESSAGE-TYPE-METADATA", "class-hierarchy")]
    public void ClassHierarchy_ExposesTheCompleteFaultTypeSet()
    {
        AssertFaultTypeSet<Fault<MemberAddressUpdated>>(
            typeof(Fault<MemberAddressUpdated>),
            typeof(Fault<MemberUpdateEvent>),
            typeof(Fault));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-MESSAGE-TYPE-METADATA", "interface-hierarchy")]
    public void InterfaceHierarchy_ExposesTheCompleteFaultTypeSet()
    {
        AssertFaultTypeSet<Fault<UpdateMemberAddress>>(
            typeof(Fault<UpdateMemberAddress>),
            typeof(Fault<MemberUpdateCommand>),
            typeof(Fault<ApplicationCommand>),
            typeof(Fault));
    }

    private static void AssertFaultTypeSet<TFault>(params Type[] expectedTypes)
        where TFault : class
    {
        IReadOnlyList<Type> actualTypes = MessageTypeCache<TFault>.MessageTypes;
        IReadOnlyList<string> actualTypeNames = MessageTypeCache<TFault>.MessageTypeNames;
        Type[] sortedExpectedTypes = expectedTypes
            .OrderBy(GetStableTypeName, StringComparer.Ordinal)
            .ToArray();
        Type[] sortedActualTypes = actualTypes
            .OrderBy(GetStableTypeName, StringComparer.Ordinal)
            .ToArray();
        string[] sortedExpectedNames = expectedTypes
            .Select(MessageUrn.ForTypeString)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] sortedActualNames = actualTypeNames
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(sortedExpectedTypes, sortedActualTypes);
        Assert.Equal(actualTypes.Count, actualTypes.Distinct().Count());
        Assert.Equal(sortedExpectedNames, sortedActualNames);
        Assert.Equal(actualTypeNames.Count, actualTypeNames.Distinct(StringComparer.Ordinal).Count());
    }

    private static string GetStableTypeName(Type type) =>
        type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

    private interface ApplicationCommand;

    private interface MemberUpdateCommand : ApplicationCommand;

    private interface UpdateMemberAddress : MemberUpdateCommand;

    private class MemberUpdateEvent;

    private sealed class MemberAddressUpdated : MemberUpdateEvent;
}
