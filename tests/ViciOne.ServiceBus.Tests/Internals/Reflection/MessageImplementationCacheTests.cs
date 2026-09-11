using ViciOne.ServiceBus.Internals.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Internals.Reflection;

public sealed class MessageImplementationCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-OWNERSHIP", "implementation-type-null-and-class-boundaries")]
    public void ImplementationTypeResolution_RejectsNullAndConcreteTypesAtItsBoundary()
    {
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            MessageImplementationCache.GetImplementationType(null!)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() =>
            MessageImplementationCache.GetImplementationType(typeof(ConcreteMessage))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-OWNERSHIP", "interface-implementation-stable-and-internal")]
    public void InterfaceResolution_ReturnsOneStableAssignableImplementationWithoutAPublicFacade()
    {
        Type first = MessageImplementationCache.GetImplementationType(typeof(InterfaceMessage));
        Type second = MessageImplementationCache<InterfaceMessage>.ImplementationType;

        Assert.Same(first, second);
        Assert.True(typeof(InterfaceMessage).IsAssignableFrom(first));
        Assert.False(first.IsInterface);
        Assert.DoesNotContain(
            typeof(MessageImplementationCache).Assembly.GetExportedTypes(),
            type => type.FullName is "ViciOne.ServiceBus.Metadata.TypeMetadataCache"
                or "ViciOne.ServiceBus.Metadata.TypeMetadataCache`1"
                or "ViciOne.ServiceBus.Internals.Reflection.MessageImplementationCache"
                or "ViciOne.ServiceBus.Internals.Reflection.MessageImplementationCache`1");
    }

    public interface InterfaceMessage
    {
        string Value { get; }
    }

    private sealed record ConcreteMessage(string Value);
}
