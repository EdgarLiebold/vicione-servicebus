using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.TypeConverters;

public sealed class DateTimeTypeConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DATETIME-CONVERTER", "datetime-minimum")]
    public void DateTimeMinimum_RoundTripsThroughTheInvariantTextForm()
    {
        Assert.True(TypeConverterCache.TryGetTypeConverter<string, DateTime>(out var toText));
        Assert.True(TypeConverterCache.TryGetTypeConverter<DateTime, string>(out var fromText));

        Assert.True(toText.TryConvert(DateTime.MinValue, out string? text));
        Assert.NotNull(text);
        Assert.Equal("0001-01-01T00:00:00.0000000", text);
        Assert.True(fromText.TryConvert(text, out DateTime result));
        Assert.Equal(DateTime.MinValue, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DATETIME-CONVERTER", "utc-minimum-to-offset")]
    public void UtcDateTimeMinimum_IsReadByTheOffsetConverterAsTheSameInstant()
    {
        Assert.True(TypeConverterCache.TryGetTypeConverter<string, DateTime>(out var dateTimeConverter));
        var offsetConverter = new DateTimeOffsetTypeConverter();
        DateTime value = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        Assert.True(dateTimeConverter.TryConvert(value, out string? text));
        Assert.NotNull(text);
        Assert.Equal("0001-01-01T00:00:00.0000000Z", text);
        Assert.True(offsetConverter.TryConvert(text, out DateTimeOffset result));
        Assert.Equal(TimeSpan.Zero, result.Offset);
        Assert.Equal(value, result.UtcDateTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DATETIME-CONVERTER", "datetime-offset-minimum")]
    public void DateTimeOffsetMinimum_RoundTripsThroughTheInvariantTextForm()
    {
        var converter = new DateTimeOffsetTypeConverter();

        Assert.True(converter.TryConvert(DateTimeOffset.MinValue, out string text));
        Assert.Equal("0001-01-01T00:00:00.0000000+00:00", text);
        Assert.True(converter.TryConvert(text, out DateTimeOffset result));
        Assert.Equal(DateTimeOffset.MinValue, result);
    }
}
