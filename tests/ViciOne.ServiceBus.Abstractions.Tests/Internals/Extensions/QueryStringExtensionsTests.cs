using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Extensions;

public sealed class QueryStringExtensionsTests
{
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
}
