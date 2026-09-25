using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Extensions;

public sealed class QueryStringExtensionsTests
{
    [Theory]
    [InlineData("rabbitmq://broker/", "/")]
    [InlineData("rabbitmq://broker/team%2Fblue", "team/blue")]
    [InlineData("rabbitmq://broker/team%2Fblue/orders", "team/blue")]
    [RequirementCoverage("REQ-VSB-QUERY-STRING", "host-path-root-and-escaped-virtual-host")]
    public void ParseHostPath_PreservesRootAndDecodesTheVirtualHost(string source, string expectedHostPath)
    {
        Assert.Equal(expectedHostPath, new Uri(source).ParseHostPath());
    }

    [Theory]
    [InlineData("rabbitmq://broker/orders%2Furgent", "/", "orders/urgent")]
    [InlineData("rabbitmq://broker/team%2Fblue/orders%2Furgent", "team/blue", "orders/urgent")]
    [RequirementCoverage("REQ-VSB-QUERY-STRING", "endpoint-path-decoding-keeps-host-and-entity-separate")]
    public void ParseHostPathAndEntityName_DecodesBothSegmentsWithoutMovingTheBoundary(
        string source, string expectedHostPath, string expectedEntityName)
    {
        new Uri(source).ParseHostPathAndEntityName(out string hostPath, out string entityName);

        Assert.Equal(expectedHostPath, hostPath);
        Assert.Equal(expectedEntityName, entityName);
    }

    [Theory]
    [InlineData("loopback:item?key=value=with=suffix", "value=with=suffix")]
    [InlineData("loopback:item?key=", "")]
    [InlineData("loopback:item?key", null)]
    [RequirementCoverage("REQ-VSB-QUERY-STRING", "split-at-first-separator")]
    public void SplitQueryString_PreservesTheCompleteRawValue(string source, string? expectedValue)
    {
        (string key, string? value) = Assert.Single(new Uri(source).SplitQueryString());

        Assert.Equal("key", key);
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUERY-STRING", "lookup-preserves-complete-value")]
    public void TryGetValueFromQueryString_PreservesTheCompleteRawValue()
    {
        var address = new Uri("loopback:item?KeY=value=with=suffix");

        bool found = address.TryGetValueFromQueryString("key", out string? value);

        Assert.True(found);
        Assert.Equal("value=with=suffix", value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUERY-STRING", "duplicate-key-rejected-case-insensitively")]
    public void TryGetValueFromQueryString_RejectsAmbiguousKeysRegardlessOfCase()
    {
        var address = new Uri("loopback:item?mode=send&Id=first&other=ignored&id=second");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            address.TryGetValueFromQueryString("iD", out _));

        Assert.Equal("The query string contains the key 'iD' more than once.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUERY-STRING", "missing-key-does-not-return-another-value")]
    public void TryGetValueFromQueryString_MissingKeyReturnsFalseAndNull()
    {
        var address = new Uri("loopback:item?mode=send&temporary=true");

        bool found = address.TryGetValueFromQueryString("id", out string? value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUERY-STRING", "valueless-present-key-is-empty")]
    public void TryGetValueFromQueryString_ValuelessKeyIsPresentWithEmptyValue()
    {
        var address = new Uri("loopback:item?id&mode=send");

        bool found = address.TryGetValueFromQueryString("id", out string? value);

        Assert.True(found);
        Assert.Equal(string.Empty, value);
    }
}
