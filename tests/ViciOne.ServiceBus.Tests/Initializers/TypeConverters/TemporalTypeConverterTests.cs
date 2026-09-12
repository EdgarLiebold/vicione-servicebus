using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.TypeConverters;

public sealed class TemporalTypeConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "datetime-bidirectional-unix-and-kind-matrix")]
    public void DateTimeConverter_CoversTextObjectUnixBoundariesAndEveryKind()
    {
        var converter = new DateTimeTypeConverter();
        DateTime epoch = DateTimeOffset.UnixEpoch.UtcDateTime;
        DateTime beforeEpoch = DateTimeOffset.FromUnixTimeMilliseconds(-1).UtcDateTime;
        DateTime intMinimum = DateTimeOffset.FromUnixTimeMilliseconds(int.MinValue).UtcDateTime;
        DateTime intMaximum = DateTimeOffset.FromUnixTimeMilliseconds(int.MaxValue).UtcDateTime;

        Assert.True(TypeConverterCache.TryGetTypeConverter<DateTime, int>(out var fromInt));
        Assert.True(fromInt.TryConvert(-1, out DateTime fromNegativeInt));
        Assert.Equal(beforeEpoch, fromNegativeInt);
        Assert.True(TypeConverterCache.TryGetTypeConverter<DateTime, long>(out var fromLong));
        Assert.True(fromLong.TryConvert(0L, out DateTime fromZeroLong));
        Assert.Equal(epoch, fromZeroLong);

        Assert.True(converter.TryConvert(0, out DateTime fromIntDirect));
        Assert.Equal(epoch, fromIntDirect);
        Assert.True(converter.TryConvert(-1L, out DateTime fromLongDirect));
        Assert.Equal(beforeEpoch, fromLongDirect);
        Assert.False(converter.TryConvert(long.MaxValue, out _));

        var offsetInput = new DateTimeOffset(2026, 9, 12, 8, 30, 0, TimeSpan.FromHours(2));
        Assert.True(converter.TryConvert(offsetInput, out DateTime fromOffset));
        Assert.Equal(offsetInput.UtcDateTime, fromOffset);

        Assert.True(converter.TryConvert((object)epoch, out DateTime fromObjectDateTime));
        Assert.Equal(epoch, fromObjectDateTime);
        Assert.True(converter.TryConvert((object)offsetInput, out DateTime fromObjectOffset));
        Assert.Equal(offsetInput.UtcDateTime, fromObjectOffset);
        Assert.True(converter.TryConvert((object)"2026-09-12T08:30:00+02:00", out DateTime fromObjectText));
        Assert.Equal(new DateTime(2026, 9, 12, 6, 30, 0, DateTimeKind.Utc), fromObjectText);
        Assert.False(converter.TryConvert((object?)null, out _));
        Assert.False(converter.TryConvert((object)" ", out _));
        Assert.False(converter.TryConvert((object)new object(), out _));
        Assert.False(converter.TryConvert("not-a-date", out _));

        Assert.True(converter.TryConvert(intMinimum, out int minimumMilliseconds));
        Assert.Equal(int.MinValue, minimumMilliseconds);
        Assert.True(converter.TryConvert(intMaximum, out int maximumMilliseconds));
        Assert.Equal(int.MaxValue, maximumMilliseconds);
        Assert.False(converter.TryConvert(intMinimum.AddMilliseconds(-1), out int _));
        Assert.False(converter.TryConvert(intMaximum.AddMilliseconds(1), out int _));
        Assert.True(converter.TryConvert(beforeEpoch, out long negativeMilliseconds));
        Assert.Equal(-1L, negativeMilliseconds);

        DateTime unspecified = DateTime.SpecifyKind(epoch.AddHours(1), DateTimeKind.Unspecified);
        Assert.True(converter.TryConvert((object)unspecified, out DateTime normalizedUnspecified));
        Assert.Equal(DateTimeKind.Utc, normalizedUnspecified.Kind);
        Assert.Equal(unspecified, normalizedUnspecified);
        Assert.True(converter.TryConvert(unspecified, out long unspecifiedMilliseconds));
        Assert.Equal(3_600_000L, unspecifiedMilliseconds);
        DateTime local = epoch.AddHours(1).ToLocalTime();
        Assert.True(converter.TryConvert(local, out long localMilliseconds));
        Assert.Equal(3_600_000L, localMilliseconds);

        Assert.True(converter.TryConvert(beforeEpoch, out string invariantText));
        Assert.Equal("1969-12-31T23:59:59.9990000Z", invariantText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "datetime-offset-bidirectional-unix-and-kind-matrix")]
    public void DateTimeOffsetConverter_CoversTextObjectUnixBoundariesAndDateTimeKinds()
    {
        var converter = new DateTimeOffsetTypeConverter();
        DateTimeOffset epoch = DateTimeOffset.UnixEpoch;
        DateTimeOffset beforeEpoch = DateTimeOffset.FromUnixTimeMilliseconds(-1);

        Assert.True(TypeConverterCache.TryGetTypeConverter<DateTimeOffset, int>(out var fromInt));
        Assert.True(fromInt.TryConvert(-1, out DateTimeOffset fromNegativeInt));
        Assert.Equal(beforeEpoch, fromNegativeInt);
        Assert.True(TypeConverterCache.TryGetTypeConverter<DateTimeOffset, long>(out var fromLong));
        Assert.True(fromLong.TryConvert(0L, out DateTimeOffset fromZeroLong));
        Assert.Equal(epoch, fromZeroLong);
        Assert.False(converter.TryConvert(long.MinValue, out DateTimeOffset _));
        Assert.False(converter.TryConvert(long.MaxValue, out DateTimeOffset _));
        Assert.True(TypeConverterCache.TryGetTypeConverter<DateTimeOffset, DateTime>(out var fromDateTime));

        DateTime unspecified = DateTime.SpecifyKind(epoch.UtcDateTime.AddHours(1), DateTimeKind.Unspecified);
        Assert.True(fromDateTime.TryConvert(unspecified, out DateTimeOffset fromUnspecified));
        Assert.Equal(TimeSpan.Zero, fromUnspecified.Offset);
        Assert.Equal(unspecified, fromUnspecified.UtcDateTime);

        DateTime local = epoch.UtcDateTime.AddHours(1).ToLocalTime();
        Assert.True(converter.TryConvert(local, out DateTimeOffset fromLocal));
        Assert.Equal(local.ToUniversalTime(), fromLocal.UtcDateTime);
        Assert.True(converter.TryConvert(epoch.UtcDateTime, out DateTimeOffset fromUtc));
        Assert.Equal(epoch, fromUtc);

        Assert.True(converter.TryConvert((object)epoch, out DateTimeOffset fromObjectOffset));
        Assert.Equal(epoch, fromObjectOffset);
        Assert.True(converter.TryConvert((object)unspecified, out DateTimeOffset fromObjectDateTime));
        Assert.Equal(TimeSpan.Zero, fromObjectDateTime.Offset);
        Assert.True(converter.TryConvert((object)"2026-09-12T08:30:00+02:00", out DateTimeOffset fromObjectText));
        Assert.Equal(new DateTimeOffset(2026, 9, 12, 8, 30, 0, TimeSpan.FromHours(2)), fromObjectText);
        Assert.False(converter.TryConvert((object?)null, out _));
        Assert.False(converter.TryConvert((object)" ", out _));
        Assert.False(converter.TryConvert((object)new object(), out _));
        Assert.False(converter.TryConvert("not-a-date", out _));

        DateTimeOffset intMinimum = DateTimeOffset.FromUnixTimeMilliseconds(int.MinValue);
        DateTimeOffset intMaximum = DateTimeOffset.FromUnixTimeMilliseconds(int.MaxValue);
        Assert.True(converter.TryConvert(intMinimum, out int minimumMilliseconds));
        Assert.Equal(int.MinValue, minimumMilliseconds);
        Assert.True(converter.TryConvert(intMaximum, out int maximumMilliseconds));
        Assert.Equal(int.MaxValue, maximumMilliseconds);
        Assert.False(converter.TryConvert(intMinimum.AddMilliseconds(-1), out int _));
        Assert.False(converter.TryConvert(intMaximum.AddMilliseconds(1), out int _));
        Assert.True(converter.TryConvert(beforeEpoch, out long negativeMilliseconds));
        Assert.Equal(-1L, negativeMilliseconds);

        Assert.True(converter.TryConvert(beforeEpoch, out string invariantText));
        Assert.Equal("1969-12-31T23:59:59.9990000+00:00", invariantText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "timespan-conversion-matrix")]
    public void TimeSpanConverter_CoversEverySupportedSourceObjectPartitionAndBoundary()
    {
        var converter = new TimeSpanTypeConverter();
        TimeSpan expected = TimeSpan.FromMilliseconds(42);

        AssertConverts<TimeSpan, byte>(converter, 42, expected);
        AssertConverts<TimeSpan, sbyte>(converter, 42, expected);
        AssertConverts<TimeSpan, short>(converter, 42, expected);
        AssertConverts<TimeSpan, ushort>(converter, 42, expected);
        AssertConverts<TimeSpan, int>(converter, 42, expected);
        AssertConverts<TimeSpan, uint>(converter, 42U, expected);
        AssertConverts<TimeSpan, long>(converter, 42L, expected);
        AssertConverts<TimeSpan, ulong>(converter, 42UL, expected);
        AssertConverts<TimeSpan, double>(converter, 42.5d, TimeSpan.FromMilliseconds(42.5d));
        AssertConverts<TimeSpan, string>(converter, "00:00:00.042", expected);
        AssertConverts<string, TimeSpan>(converter, expected, "00:00:00.0420000");

        object[] objectInputs = [
            expected,
            (sbyte)42,
            (byte)42,
            (short)42,
            (ushort)42,
            42,
            42U,
            42L,
            42UL,
            42d,
            "00:00:00.042",
        ];
        foreach (object input in objectInputs)
        {
            Assert.True(converter.TryConvert(input, out TimeSpan fromObject));
            Assert.Equal(expected, fromObject);
        }

        Assert.False(converter.TryConvert((object?)null, out _));
        Assert.False(converter.TryConvert((object)" ", out _));
        Assert.False(converter.TryConvert((object)new object(), out _));
        Assert.False(converter.TryConvert((object)ulong.MaxValue, out _));
        Assert.False(converter.TryConvert("not-a-duration", out _));
        Assert.False(converter.TryConvert(double.NegativeInfinity, out _));
        Assert.False(converter.TryConvert(double.PositiveInfinity, out _));
        Assert.False(converter.TryConvert(double.NaN, out _));
    }

    static void AssertConverts<TResult, TInput>(ITypeConverter<TResult, TInput> converter, TInput input, TResult expected)
    {
        Assert.True(converter.TryConvert(input, out TResult? result));
        Assert.Equal(expected, result);
    }
}
