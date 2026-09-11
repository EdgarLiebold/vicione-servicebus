using System.Reflection;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

public sealed class EmptyHeadersTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EMPTY-HEADERS", "immutable-singleton-contract")]
    public void Instance_IsAStableSealedEmptyHeaderCollection()
    {
        Assert.True(typeof(EmptyHeaders).IsSealed);
        PropertyInfo instanceProperty = Assert.Single(
            typeof(EmptyHeaders).GetProperties(BindingFlags.Public | BindingFlags.Static),
            property => property.Name == nameof(EmptyHeaders.Instance));
        Assert.Equal(typeof(EmptyHeaders), instanceProperty.PropertyType);
        Assert.Null(instanceProperty.SetMethod);
        Assert.Empty(typeof(EmptyHeaders).GetFields(BindingFlags.Public | BindingFlags.Static));
        Assert.Same(EmptyHeaders.Instance, EmptyHeaders.Instance);
        Assert.Empty(EmptyHeaders.Instance.GetAll());
        Assert.Empty(EmptyHeaders.Instance);
        Assert.False(EmptyHeaders.Instance.TryGetHeader("missing", out object? value));
        Assert.Null(value);
        Assert.Equal("fallback", EmptyHeaders.Instance.Get("missing", "fallback"));
        Assert.Equal(42, EmptyHeaders.Instance.Get<int>("missing", 42));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EMPTY-HEADERS", "header-name-boundaries")]
    public void Readers_RejectEveryMissingHeaderName()
    {
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => EmptyHeaders.Instance.TryGetHeader(null!, out _)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => EmptyHeaders.Instance.Get(" ", "fallback")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => EmptyHeaders.Instance.Get<int>("", 42)).ParamName);
    }
}
