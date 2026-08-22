using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Attributes;

public sealed class MessageUrnAttributeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-ATTRIBUTE-VALIDATION", "default-prefix-rejected")]
    public void Constructor_DefaultPrefixValue_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new MessageUrnAttribute("urn:message:Orders"));

        Assert.Equal("urn", exception.ParamName);
        Assert.Contains("should not contain the default prefix 'urn:message:'", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\t\t   ")]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-ATTRIBUTE-VALIDATION", "empty-or-whitespace-rejected")]
    public void Constructor_EmptyOrWhitespaceValue_IsRejected(string value)
    {
        var exception = Assert.Throws<ArgumentException>(() => new MessageUrnAttribute(value));

        Assert.Equal("urn", exception.ParamName);
        Assert.Contains("cannot be empty or whitespace", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-ATTRIBUTE-VALIDATION", "null-rejected")]
    public void Constructor_NullValue_IsRejected()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new MessageUrnAttribute(null!));

        Assert.Equal("urn", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-ATTRIBUTE-VALIDATION", "invalid-custom-uri-rejected")]
    public void Constructor_InvalidCustomUri_IsRejected()
    {
        var exception = Assert.Throws<UriFormatException>(
            () => new MessageUrnAttribute("scheme", useDefaultPrefix: false));

        Assert.Equal("Invalid URN: scheme", exception.Message);
    }
}
