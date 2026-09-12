using System.Globalization;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.TypeConverters;

public sealed class TypeConverterContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "invalid-and-overflow-return-false")]
    public void TryConverters_ReturnFalseInsteadOfThrowingForUnrepresentableInputs()
    {
        Assert.False(new BooleanTypeConverter().TryConvert((object)"not-a-boolean", out _));
        Assert.False(new ByteTypeConverter().TryConvert(256, out _));
        Assert.False(new ByteTypeConverter().TryConvert((object)"not-a-byte", out _));
        Assert.False(new IntTypeConverter().TryConvert(long.MaxValue, out _));
        Assert.False(new IntTypeConverter().TryConvert(ulong.MaxValue, out _));
        Assert.False(new LongTypeConverter().TryConvert(ulong.MaxValue, out _));
        Assert.False(new ShortTypeConverter().TryConvert(int.MaxValue, out _));
        Assert.False(new DecimalTypeConverter().TryConvert((object)"not-a-decimal", out _));
        Assert.False(new DoubleTypeConverter().TryConvert((object)"not-a-double", out _));
        Assert.False(new EnumTypeConverter<ConversionState>().TryConvert(ulong.MaxValue, out _));
        Assert.False(new TimeSpanTypeConverter().TryConvert(double.PositiveInfinity, out _));
        Assert.False(new ViciOne.ServiceBus.Initializers.TypeConverters.UriTypeConverter().TryConvert("http://[invalid", out _));
        Assert.False(new VersionTypeConverter().TryConvert("not-a-version", out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "time-span-boundaries-never-throw")]
    public void TimeSpanConversion_ReturnsFalseAtFloatingPointOverflowBoundaries()
    {
        var converter = new TimeSpanTypeConverter();

        Assert.True(converter.TryConvert(TimeSpan.MaxValue.TotalMilliseconds - 1, out _));
        Assert.True(converter.TryConvert(TimeSpan.MinValue.TotalMilliseconds + 1, out _));
        Assert.True(converter.TryConvert(TimeSpan.MaxValue.TotalMilliseconds, out TimeSpan maximum));
        Assert.InRange(maximum, TimeSpan.MaxValue - TimeSpan.FromMilliseconds(1), TimeSpan.MaxValue);
        Assert.False(converter.TryConvert(double.BitIncrement(TimeSpan.MaxValue.TotalMilliseconds), out _));
        Assert.False(converter.TryConvert(double.BitDecrement(TimeSpan.MinValue.TotalMilliseconds), out _));
        Assert.False(converter.TryConvert(double.NaN, out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "invariant-numeric-text")]
    public void NumericTextConversion_IsIndependentOfTheCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";

        try
        {
            CultureInfo.CurrentCulture = culture;
            var converter = new IntTypeConverter();

            Assert.False(converter.TryConvert("~1", out _));
            Assert.True(converter.TryConvert("-1", out int value));
            Assert.Equal(-1, value);
            Assert.True(converter.TryConvert(-1, out string? text));
            Assert.Equal("-1", text);
            Assert.True(new StringTypeConverter().TryConvert((object)(-1), out string? objectText));
            Assert.Equal("-1", objectText);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "offset-date-time-normalizes-to-utc")]
    public void OffsetTextConvertedToDateTime_RepresentsTheSameUtcInstant()
    {
        Assert.True(TypeConverterCache.TryGetTypeConverter<DateTime, string>(out var converter));
        const string text = "2024-01-02T03:04:05.0000000+02:00";

        Assert.True(converter.TryConvert(text, out DateTime result));

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(new DateTime(2024, 1, 2, 1, 4, 5, DateTimeKind.Utc), result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "nullable-enum-resolution-is-order-independent")]
    public void NullableEnumConverter_IsResolvedWithoutWarmingTheUnderlyingEnumCache()
    {
        Assert.True(TypeConverterCache.TryGetTypeConverter<ColdConversionState?, string>(out var converter));

        Assert.True(converter.TryConvert(nameof(ColdConversionState.Ready), out ColdConversionState? result));
        Assert.Equal(ColdConversionState.Ready, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "nullable-adapters-require-underlying-converter")]
    public void NullableConverterAdapters_RejectMissingUnderlyingConverters()
    {
        Assert.Equal("typeConverter", Assert.Throws<ArgumentNullException>(() =>
            new ToNullableTypeConverter<int, string>(null!)).ParamName);
        Assert.Equal("typeConverter", Assert.Throws<ArgumentNullException>(() =>
            new FromNullableTypeConverter<string, int>(null!)).ParamName);
    }

    private enum ConversionState
    {
        Unknown,
        Ready,
    }

    private enum ColdConversionState
    {
        Unknown,
        Ready,
    }
}
