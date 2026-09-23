using System.Reflection;
using System.Reflection.Emit;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Metadata;

public sealed class MessageTypeCacheImmutabilityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-IMMUTABILITY", "read-only-public-contract")]
    public void PublicMetadataCollections_ExposeReadOnlyListContracts()
    {
        Assert.True(typeof(MessageTypeCache<CacheMessage>).IsSealed);
        Assert.Equal(
            typeof(IReadOnlyList<Type>),
            typeof(MessageTypeCache<CacheMessage>).GetProperty(nameof(MessageTypeCache<CacheMessage>.MessageTypes))!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<string>),
            typeof(MessageTypeCache<CacheMessage>).GetProperty(nameof(MessageTypeCache<CacheMessage>.MessageTypeNames))!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<PropertyInfo>),
            typeof(MessageTypeCache<CacheMessage>).GetProperty(nameof(MessageTypeCache<CacheMessage>.Properties))!.PropertyType);

        Assert.Equal(
            typeof(IReadOnlyList<Type>),
            typeof(MessageTypeCache).GetMethod(nameof(MessageTypeCache.GetMessageTypes))!.ReturnType);
        Assert.Equal(
            typeof(IReadOnlyList<string>),
            typeof(MessageTypeCache).GetMethod(nameof(MessageTypeCache.GetMessageTypeNames))!.ReturnType);
        Assert.Equal(
            typeof(IReadOnlyList<PropertyInfo>),
            typeof(MessageTypeCache).GetMethod(nameof(MessageTypeCache.GetProperties))!.ReturnType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-IMMUTABILITY", "runtime-type-is-required")]
    public void RuntimeTypeApis_RejectAMissingTypeConsistently()
    {
        Action[] calls =
        [
            () => MessageTypeCache.GetProperties(null!),
            () => MessageTypeCache.IsValidMessageType(null!),
            () => MessageTypeCache.InvalidMessageTypeReason(null!),
            () => MessageTypeCache.IsTemporaryMessageType(null!),
            () => MessageTypeCache.GetMessageTypes(null!),
            () => MessageTypeCache.GetMessageTypeNames(null!),
        ];

        foreach (Action call in calls)
            Assert.Equal("type", Assert.Throws<ArgumentNullException>(call).ParamName);
    }

    [Theory]
    [InlineData(typeof(int), "reference types")]
    [InlineData(typeof(Action), "Delegates")]
    [InlineData(typeof(OpenGenericMessage<>), "open generic")]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "invalid-runtime-contract-shapes")]
    public void RuntimeTypeApis_ReturnConsistentMetadataForInvalidContractShapes(Type invalidType, string reasonFragment)
    {
        Assert.False(MessageTypeCache.IsValidMessageType(invalidType));
        Assert.False(MessageTypeCache.IsTemporaryMessageType(invalidType));
        Assert.Contains(reasonFragment, MessageTypeCache.InvalidMessageTypeReason(invalidType), StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache.GetProperties(invalidType));
        Assert.Empty(MessageTypeCache.GetMessageTypes(invalidType));
        Assert.Empty(MessageTypeCache.GetMessageTypeNames(invalidType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "invalid-generic-contract-shapes")]
    public void GenericCache_ReturnsConsistentMetadataForInvalidContractShapes()
    {
        Assert.False(MessageTypeCache<int>.IsValidMessageType);
        Assert.False(MessageTypeCache<int>.IsTemporaryMessageType);
        Assert.Contains("reference types", MessageTypeCache<int>.InvalidMessageTypeReason, StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache<int>.Properties);
        Assert.Empty(MessageTypeCache<int>.MessageTypes);
        Assert.Empty(MessageTypeCache<int>.MessageTypeNames);

        Assert.False(MessageTypeCache<Action>.IsValidMessageType);
        Assert.False(MessageTypeCache<Action>.IsTemporaryMessageType);
        Assert.Contains("Delegates", MessageTypeCache<Action>.InvalidMessageTypeReason, StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache<Action>.Properties);
        Assert.Empty(MessageTypeCache<Action>.MessageTypes);
        Assert.Empty(MessageTypeCache<Action>.MessageTypeNames);
    }

    [Theory]
    [InlineData(typeof(SendContext))]
    [InlineData(typeof(ConsumeContext))]
    [InlineData(typeof(ReceiveContext))]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "infrastructure-context-contracts-rejected")]
    public void InfrastructureContexts_CannotBecomeMessageContracts(Type contextType)
    {
        Assert.False(MessageTypeCache.IsValidMessageType(contextType));
        Assert.Contains("not valid message types", MessageTypeCache.InvalidMessageTypeReason(contextType), StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache.GetMessageTypes(contextType));
        Assert.Empty(MessageTypeCache.GetMessageTypeNames(contextType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "closed-correlation-interface-excluded")]
    public void MarkedCorrelationInterface_IsExcludedEvenAfterClosingItsGenericArgument()
    {
        Type correlationType = typeof(IMessageCorrelation<Guid>);

        Assert.False(MessageTypeCache.IsValidMessageType(correlationType));
        Assert.Contains("not a valid message type", MessageTypeCache.InvalidMessageTypeReason(correlationType), StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache.GetMessageTypes(correlationType));
        Assert.Empty(MessageTypeCache.GetMessageTypeNames(correlationType));
        Assert.Equal([typeof(CorrelatedMessage)], MessageTypeCache<CorrelatedMessage>.MessageTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "framework-json-object-exception")]
    public void JsonObject_RemainsAValidContractDespiteItsSystemNamespace()
    {
        Type jsonObjectType = typeof(System.Text.Json.Nodes.JsonObject);

        Assert.True(MessageTypeCache.IsValidMessageType(jsonObjectType));
        Assert.Null(MessageTypeCache.InvalidMessageTypeReason(jsonObjectType));
        Assert.Equal([jsonObjectType], MessageTypeCache.GetMessageTypes(jsonObjectType));
        Assert.Equal([MessageUrn.ForTypeString(jsonObjectType)], MessageTypeCache.GetMessageTypeNames(jsonObjectType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "foreign-json-object-lookalike-rejected")]
    public void AForeignJsonObjectWithTheSameFullName_DoesNotGainTheFrameworkException()
    {
        Type lookalike = CreateDynamicType("System.Text.Json.Nodes.JsonObject");

        Assert.NotEqual(typeof(System.Text.Json.Nodes.JsonObject), lookalike);
        Assert.False(MessageTypeCache.IsValidMessageType(lookalike));
        Assert.Contains("System namespace", MessageTypeCache.InvalidMessageTypeReason(lookalike), StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache.GetMessageTypes(lookalike));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "namespace-required-for-nonanonymous-types")]
    public void ATypeWithoutANamespace_ReportsTheNamespaceFailure()
    {
        Type nameless = CreateDynamicType("NamelessMessageContract");

        Assert.Null(nameless.Namespace);
        Assert.False(MessageTypeCache.IsValidMessageType(nameless));
        Assert.Contains("valid namespace", MessageTypeCache.InvalidMessageTypeReason(nameless), StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache.GetMessageTypes(nameless));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "corelib-type-outside-system-namespace-rejected")]
    public void ACoreLibraryTypeOutsideSystemNamespace_RemainsInvalid()
    {
        Type coreLibraryType = typeof(Microsoft.Win32.SafeHandles.SafeFileHandle);

        Assert.Equal(typeof(object).Assembly, coreLibraryType.Assembly);
        Assert.False(MessageTypeCache.IsValidMessageType(coreLibraryType));
        Assert.Contains("System types", MessageTypeCache.InvalidMessageTypeReason(coreLibraryType), StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache.GetMessageTypes(coreLibraryType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-MESSAGE-TYPE-METADATA", "invalid-inner-fault-retains-own-and-base-contracts")]
    public void FaultOfAnInvalidContext_RetainsOnlyItsOwnAndBaseFaultContracts()
    {
        Type[] expected = [typeof(Fault<ConsumeContext>), typeof(Fault)];

        Assert.Equal(expected, MessageTypeCache<Fault<ConsumeContext>>.MessageTypes);
        Assert.Equal(expected.Select(MessageUrn.ForTypeString), MessageTypeCache<Fault<ConsumeContext>>.MessageTypeNames);
        Assert.DoesNotContain(typeof(Fault<PipeContext>), MessageTypeCache<Fault<ConsumeContext>>.MessageTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "json-document-does-not-inherit-exception")]
    public void JsonDocument_DoesNotInheritTheJsonObjectContractException()
    {
        Type jsonDocumentType = typeof(System.Text.Json.JsonDocument);

        Assert.False(MessageTypeCache.IsValidMessageType(jsonDocumentType));
        Assert.Contains("System namespace", MessageTypeCache.InvalidMessageTypeReason(jsonDocumentType), StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache.GetMessageTypes(jsonDocumentType));
        Assert.Empty(MessageTypeCache.GetMessageTypeNames(jsonDocumentType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "anonymous-contract-rejected")]
    public void AnonymousType_IsRejectedWithItsSpecificReason()
    {
        Type anonymousType = new { Value = "message" }.GetType();

        Assert.False(MessageTypeCache.IsValidMessageType(anonymousType));
        Assert.Contains("anonymous types", MessageTypeCache.InvalidMessageTypeReason(anonymousType), StringComparison.Ordinal);
        Assert.Empty(MessageTypeCache.GetMessageTypes(anonymousType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-IMMUTABILITY", "cached-state-cannot-be-mutated")]
    public void CachedCollections_RejectMutationAndRemainStableAcrossReaders()
    {
        object messageTypes = MessageTypeCache<CacheMessage>.MessageTypes;
        object messageTypeNames = MessageTypeCache<CacheMessage>.MessageTypeNames;
        object properties = MessageTypeCache<CacheMessage>.Properties;

        Assert.IsNotType<Type[]>(messageTypes);
        Assert.IsNotType<string[]>(messageTypeNames);
        Assert.IsNotType<List<PropertyInfo>>(properties);

        IList<Type> mutableTypes = Assert.IsAssignableFrom<IList<Type>>(messageTypes);
        IList<string> mutableNames = Assert.IsAssignableFrom<IList<string>>(messageTypeNames);
        IList<PropertyInfo> mutableProperties = Assert.IsAssignableFrom<IList<PropertyInfo>>(properties);
        Assert.Throws<NotSupportedException>(() => mutableTypes[0] = typeof(string));
        Assert.Throws<NotSupportedException>(() => mutableNames[0] = "urn:poisoned");
        Assert.Throws<NotSupportedException>(() => mutableProperties.Clear());

        IReadOnlyList<Type> expectedTypes = Assert.IsAssignableFrom<IReadOnlyList<Type>>(messageTypes);
        IReadOnlyList<string> expectedNames = Assert.IsAssignableFrom<IReadOnlyList<string>>(messageTypeNames);
        IReadOnlyList<PropertyInfo> expectedProperties = Assert.IsAssignableFrom<IReadOnlyList<PropertyInfo>>(properties);
        Parallel.For(0, 64, _ =>
        {
            Assert.Equal(expectedTypes, MessageTypeCache<CacheMessage>.MessageTypes);
            Assert.Equal(expectedNames, MessageTypeCache<CacheMessage>.MessageTypeNames);
            Assert.Equal(expectedProperties, MessageTypeCache<CacheMessage>.Properties);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "custom-diagnostic-addresses")]
    public void DiagnosticAddress_PreservesEachSupportedCustomUrnShape()
    {
        Assert.Equal("ContractName", MessageTypeCache<NameOnlyUrnMessage>.DiagnosticAddress);
        Assert.Equal("ContractName/Contracts/AssemblyScope", MessageTypeCache<AssemblyScopedUrnMessage>.DiagnosticAddress);
        Assert.Equal("scheme:identifier", MessageTypeCache<CustomSchemeMessage>.DiagnosticAddress);
    }

    private interface CacheContract;

    private sealed record CacheMessage(string Value) : CacheContract;

    private sealed class CorrelatedMessage : IMessageCorrelation<Guid>
    {
        public Guid CorrelationId { get; } = Guid.Empty;
    }

    [MessageUrn("ContractName")]
    private sealed class NameOnlyUrnMessage;

    [MessageUrn("Contracts:ContractName:AssemblyScope")]
    private sealed class AssemblyScopedUrnMessage;

    [MessageUrn("scheme:identifier", useDefaultPrefix: false)]
    private sealed class CustomSchemeMessage;

    private sealed class OpenGenericMessage<T>;

    private static Type CreateDynamicType(string fullName)
    {
        var assemblyName = new AssemblyName($"ViciOne.ServiceBus.Tests.ContractBoundary.{fullName}");
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule("Contracts");
        return module.DefineType(fullName, TypeAttributes.Public | TypeAttributes.Class).CreateType()!;
    }
}
