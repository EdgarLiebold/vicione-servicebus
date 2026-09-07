using System.Reflection;
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

    private interface CacheContract;

    private sealed record CacheMessage(string Value) : CacheContract;

    private sealed class OpenGenericMessage<T>;
}
