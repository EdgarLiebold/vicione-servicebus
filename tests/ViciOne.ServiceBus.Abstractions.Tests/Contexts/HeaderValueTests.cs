using System.Globalization;
using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Contexts;

public sealed class HeaderValueTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-HEADER-VALUE", "required-name-and-value")]
    public void Constructors_RejectMissingNamesAndValues()
    {
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => new HeaderValue(" ", 1)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => new HeaderValue("name", null!)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => new HeaderValue<string>(" ", "value")).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => new HeaderValue<string>("name", null!)).ParamName);
        Assert.Equal(
            "key",
            Assert.Throws<ArgumentException>(() => new HeaderValue(new KeyValuePair<string, object>(" ", 1))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HEADER-VALUE", "false-boolean-is-preserved")]
    public void BooleanFalse_IsAValidSimpleAndStringHeaderValue()
    {
        var header = new HeaderValue("enabled", false);

        Assert.True(header.IsSimpleValue(out HeaderValue simple));
        Assert.Equal(false, simple.Value);
        Assert.True(header.IsStringValue(out HeaderValue<string> text));
        Assert.Equal(bool.FalseString, text.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HEADER-VALUE", "value-formatting-is-invariant")]
    public void FormattableValue_UsesInvariantWireText()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var header = new HeaderValue<double>("amount", 12.5);

            Assert.True(header.IsStringValue(out HeaderValue<string> text));
            Assert.Equal("12.5", text.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HEADER-VALUE", "unsupported-reference-is-not-scalar")]
    public void UnsupportedReferenceValue_IsNotReportedAsTransportScalar()
    {
        var header = new HeaderValue("complex", new object());

        Assert.False(header.IsStringValue(out _));
        Assert.False(header.IsSimpleValue(out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HEADER-VALUE", "read-only-descriptive-public-metadata")]
    public void Contract_UsesReadOnlyPropertiesAndDescriptiveGenericMetadata()
    {
        Assert.Empty(typeof(HeaderValue).GetFields(BindingFlags.Instance | BindingFlags.Public));
        Assert.Empty(typeof(HeaderValue<>).GetFields(BindingFlags.Instance | BindingFlags.Public));
        Assert.Equal("TValue", Assert.Single(typeof(HeaderValue<>).GetGenericArguments()).Name);
        Assert.All(typeof(HeaderValue).GetProperties(), property => Assert.False(property.CanWrite));
        Assert.All(typeof(HeaderValue<>).GetProperties(), property => Assert.False(property.CanWrite));
    }
}
