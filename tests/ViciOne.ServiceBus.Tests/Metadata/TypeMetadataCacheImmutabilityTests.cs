using System.Reflection;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Metadata;

public sealed class TypeMetadataCacheImmutabilityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-IMMUTABILITY", "core-facade-preserves-read-only-contract")]
    public void PublicFacade_PreservesTheReadOnlyStableMetadataContract()
    {
        Assert.Equal(
            typeof(IReadOnlyList<Type>),
            typeof(TypeMetadataCache<CacheMessage>).GetProperty(nameof(TypeMetadataCache<CacheMessage>.MessageTypes))!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<string>),
            typeof(TypeMetadataCache<CacheMessage>).GetProperty(nameof(TypeMetadataCache<CacheMessage>.MessageTypeNames))!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<PropertyInfo>),
            typeof(TypeMetadataCache<CacheMessage>).GetProperty(nameof(TypeMetadataCache<CacheMessage>.Properties))!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<Type>),
            typeof(TypeMetadataCache).GetMethod(nameof(TypeMetadataCache.GetMessageTypes))!.ReturnType);
        Assert.Equal(
            typeof(IReadOnlyList<string>),
            typeof(TypeMetadataCache).GetMethod(nameof(TypeMetadataCache.GetMessageTypeNames))!.ReturnType);
        Assert.Equal(
            typeof(IReadOnlyList<PropertyInfo>),
            typeof(TypeMetadataCache).GetMethod(nameof(TypeMetadataCache.GetProperties))!.ReturnType);

        IReadOnlyList<Type> messageTypes = TypeMetadataCache<CacheMessage>.MessageTypes;
        IReadOnlyList<string> messageTypeNames = TypeMetadataCache<CacheMessage>.MessageTypeNames;
        IReadOnlyList<PropertyInfo> properties = TypeMetadataCache<CacheMessage>.Properties;

        Assert.IsNotType<Type[]>(messageTypes);
        Assert.IsNotType<string[]>(messageTypeNames);
        Assert.IsNotType<PropertyInfo[]>(properties);
        Assert.Same(messageTypes, TypeMetadataCache.GetMessageTypes(typeof(CacheMessage)));
        Assert.Same(messageTypeNames, TypeMetadataCache.GetMessageTypeNames(typeof(CacheMessage)));
        Assert.Same(properties, TypeMetadataCache.GetProperties(typeof(CacheMessage)));
        Assert.Throws<NotSupportedException>(() => Assert.IsAssignableFrom<IList<Type>>(messageTypes).Clear());
        Assert.Throws<NotSupportedException>(() => Assert.IsAssignableFrom<IList<string>>(messageTypeNames).Clear());
        Assert.Throws<NotSupportedException>(() => Assert.IsAssignableFrom<IList<PropertyInfo>>(properties).Clear());
    }

    private interface CacheContract;

    private sealed record CacheMessage(string Value) : CacheContract;
}
