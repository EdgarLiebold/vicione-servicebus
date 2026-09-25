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
    [RequirementCoverage("REQ-VSB-HEADER-VALUE", "default-value-is-not-a-wire-header")]
    public void DefaultHeaders_CannotProduceWireValues()
    {
        HeaderValue untyped = default;
        HeaderValue<string> typed = default;
        HeaderValue<int> number = default;
        HeaderValue<bool> flag = default;

        Assert.False(untyped.IsStringValue(out HeaderValue<string> untypedText));
        Assert.Equal(default, untypedText);
        Assert.False(untyped.IsSimpleValue(out HeaderValue untypedScalar));
        Assert.Equal(default, untypedScalar);
        Assert.False(typed.IsStringValue(out HeaderValue<string> typedText));
        Assert.Equal(default, typedText);
        Assert.False(typed.IsSimpleValue(out HeaderValue typedScalar));
        Assert.Equal(default, typedScalar);
        Assert.False(number.IsStringValue(out HeaderValue<string> numberText));
        Assert.Equal(default, numberText);
        Assert.False(number.IsSimpleValue(out HeaderValue numberScalar));
        Assert.Equal(default, numberScalar);
        Assert.False(flag.IsStringValue(out HeaderValue<string> flagText));
        Assert.Equal(default, flagText);
        Assert.False(flag.IsSimpleValue(out HeaderValue flagScalar));
        Assert.Equal(default, flagScalar);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HEADER-VALUE", "typed-string-retains-wire-value")]
    public void TypedStringHeader_RetainsItsKeyAndExactWireValue()
    {
        var header = new HeaderValue<string>("correlation-id", "  abc-123  ");

        Assert.True(header.IsStringValue(out HeaderValue<string> text));
        Assert.Equal("correlation-id", text.Key);
        Assert.Equal("  abc-123  ", text.Value);
        Assert.True(header.IsSimpleValue(out HeaderValue scalar));
        Assert.Equal("correlation-id", scalar.Key);
        Assert.Equal("  abc-123  ", scalar.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HEADER-VALUE", "uri-wire-text-and-key")]
    public void UriHeader_UsesTextForBothTransportRepresentationsAndRetainsItsKey()
    {
        var header = new HeaderValue("destination", new Uri("https://example.test/path?q=1"));

        Assert.True(header.IsStringValue(out HeaderValue<string> text));
        Assert.Equal("destination", text.Key);
        Assert.Equal("https://example.test/path?q=1", text.Value);
        Assert.True(header.IsSimpleValue(out HeaderValue scalar));
        Assert.Equal("destination", scalar.Key);
        Assert.Equal(text.Value, scalar.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HEADER-VALUE", "null-formattable-text-is-rejected")]
    public void FormattableWithoutText_IsNotSerializedAsTextButRemainsAValueScalar()
    {
        var value = new NullTextFormattable();
        var header = new HeaderValue<NullTextFormattable>("opaque", value);

        Assert.False(header.IsStringValue(out HeaderValue<string> text));
        Assert.Equal(default, text);
        Assert.True(header.IsSimpleValue(out HeaderValue scalar));
        Assert.Equal("opaque", scalar.Key);
        Assert.Equal(value, scalar.Value);
    }

    private readonly struct NullTextFormattable : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => null!;
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
