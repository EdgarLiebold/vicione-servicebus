using System.Globalization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Serialization;

public sealed class CamelCaseDictionaryExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CAMEL-CASE-METADATA", "implementation-helper-remains-internal")]
    public void Helper_RemainsInternalToSerializationMetadataLookup()
    {
        Assert.False(typeof(CamelCaseDictionaryExtensions).IsPublic);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CAMEL-CASE-METADATA", "both-dictionary-contracts-use-invariant-key-normalization")]
    public void BothOverloads_ApplyInvariantCamelCaseNormalization()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            IDictionary<string, object> mutable = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["identifier"] = 27,
                ["urlValue"] = 73,
            };
            IReadOnlyDictionary<string, object> readOnly = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["identifier"] = 42,
                ["urlValue"] = 91,
            };

            Assert.True(mutable.TryGetValueCamelCase("Identifier", out object? mutableValue));
            Assert.Equal(27, mutableValue);
            Assert.True(readOnly.TryGetValueCamelCase("Identifier", out object? readOnlyValue));
            Assert.Equal(42, readOnlyValue);
            Assert.True(mutable.TryGetValueCamelCase("URLValue", out mutableValue));
            Assert.Equal(73, mutableValue);
            Assert.True(readOnly.TryGetValueCamelCase("URLValue", out readOnlyValue));
            Assert.Equal(91, readOnlyValue);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-CAMEL-CASE-METADATA", "both-dictionary-contracts-reject-missing-keys")]
    public void BothOverloads_RejectAMissingKey(string? key)
    {
        IDictionary<string, object> mutable = new Dictionary<string, object>();
        IReadOnlyDictionary<string, object> readOnly = new Dictionary<string, object>();

        ArgumentException mutableException = Assert.ThrowsAny<ArgumentException>(
            () => mutable.TryGetValueCamelCase(key!, out _));
        ArgumentException readOnlyException = Assert.ThrowsAny<ArgumentException>(
            () => readOnly.TryGetValueCamelCase(key!, out _));

        Assert.Equal("key", mutableException.ParamName);
        Assert.Equal("key", readOnlyException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CAMEL-CASE-METADATA", "nullable-dictionaries-and-non-pascal-keys-report-absence")]
    public void NullableDictionariesAndNonPascalKeys_ReportAbsenceWithoutAValue()
    {
        IDictionary<string, object>? mutable = null;
        IReadOnlyDictionary<string, object>? readOnly = null;

        Assert.False(mutable.TryGetValueCamelCase("Key", out object? missingMutableValue));
        Assert.Null(missingMutableValue);
        Assert.False(readOnly.TryGetValueCamelCase("Key", out object? missingReadOnlyValue));
        Assert.Null(missingReadOnlyValue);

        mutable = new Dictionary<string, object> { ["alreadyCamel"] = "value" };
        readOnly = new Dictionary<string, object> { ["alreadyCamel"] = "value" };

        Assert.False(mutable.TryGetValueCamelCase("alreadyCamel", out missingMutableValue));
        Assert.Null(missingMutableValue);
        Assert.False(readOnly.TryGetValueCamelCase("alreadyCamel", out missingReadOnlyValue));
        Assert.Null(missingReadOnlyValue);
    }
}
