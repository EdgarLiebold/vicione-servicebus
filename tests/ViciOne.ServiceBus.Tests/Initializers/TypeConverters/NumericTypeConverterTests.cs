using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.TypeConverters;

public sealed class NumericTypeConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "boolean-conversion-matrix")]
    public void BooleanConverter_CoversEverySupportedSourceAndInvalidObjectPartition()
    {
        var converter = new BooleanTypeConverter();

        AssertConverts<bool, byte>(converter, 0, false);
        AssertConverts<bool, int>(converter, -1, true);
        AssertConverts<bool, long>(converter, 1L, true);
        AssertConverts<bool, sbyte>(converter, -1, true);
        AssertConverts<bool, short>(converter, 1, true);
        AssertConverts<bool, uint>(converter, 0U, false);
        AssertConverts<bool, ulong>(converter, 1UL, true);
        AssertConverts<bool, ushort>(converter, 0, false);
        AssertConverts<bool, string>(converter, "true", true);
        AssertConverts<bool, object>(converter, 1, true);
        AssertConverts<string, bool>(converter, false, "False");
        AssertDoesNotConvert<bool, string>(converter, "not-a-boolean");
        AssertDoesNotConvert<bool, object>(converter, null);
        AssertDoesNotConvert<bool, object>(converter, new object());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "byte-conversion-matrix")]
    public void ByteConverter_CoversEverySupportedSourceAndRangeBoundary()
    {
        var converter = new ByteTypeConverter();

        AssertConverts<byte, int>(converter, byte.MinValue, byte.MinValue);
        AssertConverts<byte, int>(converter, byte.MaxValue, byte.MaxValue);
        AssertDoesNotConvert<byte, int>(converter, -1);
        AssertDoesNotConvert<byte, int>(converter, byte.MaxValue + 1);
        AssertConverts<byte, long>(converter, byte.MaxValue, byte.MaxValue);
        AssertDoesNotConvert<byte, long>(converter, -1L);
        AssertDoesNotConvert<byte, long>(converter, byte.MaxValue + 1L);
        AssertConverts<byte, sbyte>(converter, 0, 0);
        AssertDoesNotConvert<byte, sbyte>(converter, -1);
        AssertConverts<byte, short>(converter, byte.MaxValue, byte.MaxValue);
        AssertDoesNotConvert<byte, short>(converter, -1);
        AssertDoesNotConvert<byte, short>(converter, byte.MaxValue + 1);
        AssertConverts<byte, uint>(converter, byte.MaxValue, byte.MaxValue);
        AssertDoesNotConvert<byte, uint>(converter, byte.MaxValue + 1U);
        AssertConverts<byte, ulong>(converter, byte.MaxValue, byte.MaxValue);
        AssertDoesNotConvert<byte, ulong>(converter, byte.MaxValue + 1UL);
        AssertConverts<byte, ushort>(converter, byte.MaxValue, byte.MaxValue);
        AssertDoesNotConvert<byte, ushort>(converter, byte.MaxValue + 1);
        AssertConverts<byte, string>(converter, "255", byte.MaxValue);
        AssertDoesNotConvert<byte, string>(converter, "256");
        AssertConverts<byte, object>(converter, "42", 42);
        AssertDoesNotConvert<byte, object>(converter, null);
        AssertDoesNotConvert<byte, object>(converter, "invalid");
        AssertDoesNotConvert<byte, object>(converter, new object());
        AssertDoesNotConvert<byte, object>(converter, 256);
        AssertConverts<string, byte>(converter, 42, "42");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "short-conversion-matrix")]
    public void ShortConverter_CoversEverySupportedSourceAndRangeBoundary()
    {
        var converter = new ShortTypeConverter();

        AssertConverts<short, byte>(converter, byte.MaxValue, byte.MaxValue);
        AssertConverts<short, sbyte>(converter, sbyte.MinValue, sbyte.MinValue);
        AssertConverts<short, int>(converter, short.MinValue, short.MinValue);
        AssertConverts<short, int>(converter, short.MaxValue, short.MaxValue);
        AssertDoesNotConvert<short, int>(converter, short.MinValue - 1);
        AssertDoesNotConvert<short, int>(converter, short.MaxValue + 1);
        AssertConverts<short, long>(converter, short.MinValue, short.MinValue);
        AssertDoesNotConvert<short, long>(converter, short.MinValue - 1L);
        AssertDoesNotConvert<short, long>(converter, short.MaxValue + 1L);
        AssertConverts<short, uint>(converter, (uint)short.MaxValue, short.MaxValue);
        AssertDoesNotConvert<short, uint>(converter, (uint)short.MaxValue + 1);
        AssertConverts<short, ulong>(converter, (ulong)short.MaxValue, short.MaxValue);
        AssertDoesNotConvert<short, ulong>(converter, (ulong)short.MaxValue + 1);
        AssertConverts<short, ushort>(converter, (ushort)short.MaxValue, short.MaxValue);
        AssertDoesNotConvert<short, ushort>(converter, (ushort)(short.MaxValue + 1));
        AssertConverts<short, string>(converter, "-32768", short.MinValue);
        AssertDoesNotConvert<short, string>(converter, "32768");
        AssertConverts<short, object>(converter, "42", 42);
        AssertDoesNotConvert<short, object>(converter, null);
        AssertDoesNotConvert<short, object>(converter, "invalid");
        AssertDoesNotConvert<short, object>(converter, new object());
        AssertDoesNotConvert<short, object>(converter, 32_768);
        AssertConverts<string, short>(converter, -42, "-42");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "int-conversion-matrix")]
    public void IntConverter_CoversEverySupportedSourceAndRangeBoundary()
    {
        var converter = new IntTypeConverter();

        AssertConverts<int, byte>(converter, byte.MaxValue, byte.MaxValue);
        AssertConverts<int, sbyte>(converter, sbyte.MinValue, sbyte.MinValue);
        AssertConverts<int, short>(converter, short.MinValue, short.MinValue);
        AssertConverts<int, ushort>(converter, ushort.MaxValue, ushort.MaxValue);
        AssertConverts<int, long>(converter, int.MinValue, int.MinValue);
        AssertConverts<int, long>(converter, int.MaxValue, int.MaxValue);
        AssertDoesNotConvert<int, long>(converter, (long)int.MinValue - 1);
        AssertDoesNotConvert<int, long>(converter, (long)int.MaxValue + 1);
        AssertConverts<int, uint>(converter, int.MaxValue, int.MaxValue);
        AssertDoesNotConvert<int, uint>(converter, (uint)int.MaxValue + 1);
        AssertConverts<int, ulong>(converter, int.MaxValue, int.MaxValue);
        AssertDoesNotConvert<int, ulong>(converter, (ulong)int.MaxValue + 1);
        AssertConverts<int, string>(converter, "-2147483648", int.MinValue);
        AssertDoesNotConvert<int, string>(converter, "2147483648");
        AssertConverts<int, object>(converter, "42", 42);
        AssertDoesNotConvert<int, object>(converter, null);
        AssertDoesNotConvert<int, object>(converter, "invalid");
        AssertDoesNotConvert<int, object>(converter, new object());
        AssertDoesNotConvert<int, object>(converter, long.MaxValue);
        AssertConverts<string, int>(converter, -42, "-42");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "long-conversion-matrix")]
    public void LongConverter_CoversEverySupportedSourceAndUnsignedBoundary()
    {
        var converter = new LongTypeConverter();

        AssertConverts<long, byte>(converter, byte.MaxValue, byte.MaxValue);
        AssertConverts<long, sbyte>(converter, sbyte.MinValue, sbyte.MinValue);
        AssertConverts<long, short>(converter, short.MinValue, short.MinValue);
        AssertConverts<long, ushort>(converter, ushort.MaxValue, ushort.MaxValue);
        AssertConverts<long, int>(converter, int.MinValue, int.MinValue);
        AssertConverts<long, uint>(converter, uint.MaxValue, uint.MaxValue);
        AssertConverts<long, ulong>(converter, long.MaxValue, long.MaxValue);
        AssertDoesNotConvert<long, ulong>(converter, (ulong)long.MaxValue + 1);
        AssertConverts<long, string>(converter, "-9223372036854775808", long.MinValue);
        AssertDoesNotConvert<long, string>(converter, "9223372036854775808");
        AssertConverts<long, object>(converter, "42", 42);
        AssertDoesNotConvert<long, object>(converter, null);
        AssertDoesNotConvert<long, object>(converter, "invalid");
        AssertDoesNotConvert<long, object>(converter, new object());
        AssertDoesNotConvert<long, object>(converter, ulong.MaxValue);
        AssertConverts<string, long>(converter, -42, "-42");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "floating-point-and-decimal-conversion-matrix")]
    public void FloatingPointConverters_CoverEverySupportedSourceAndInvalidObjectPartition()
    {
        var decimalConverter = new DecimalTypeConverter();
        AssertConverts<decimal, byte>(decimalConverter, 1, 1m);
        AssertConverts<decimal, sbyte>(decimalConverter, -1, -1m);
        AssertConverts<decimal, short>(decimalConverter, -2, -2m);
        AssertConverts<decimal, ushort>(decimalConverter, 2, 2m);
        AssertConverts<decimal, int>(decimalConverter, -3, -3m);
        AssertConverts<decimal, uint>(decimalConverter, 3U, 3m);
        AssertConverts<decimal, long>(decimalConverter, -4L, -4m);
        AssertConverts<decimal, ulong>(decimalConverter, 4UL, 4m);
        AssertConverts<decimal, string>(decimalConverter, "867.53", 867.53m);
        AssertConverts<decimal, string>(decimalConverter, "867,53", 86_753m);
        AssertConverts<decimal, object>(decimalConverter, "42.5", 42.5m);
        AssertDoesNotConvert<decimal, object>(decimalConverter, null);
        AssertDoesNotConvert<decimal, object>(decimalConverter, "invalid");
        AssertDoesNotConvert<decimal, object>(decimalConverter, new object());
        AssertDoesNotConvert<decimal, object>(decimalConverter, double.MaxValue);
        AssertConverts<string, decimal>(decimalConverter, -42.5m, "-42.5");

        var doubleConverter = new DoubleTypeConverter();
        AssertConverts<double, byte>(doubleConverter, 1, 1d);
        AssertConverts<double, sbyte>(doubleConverter, -1, -1d);
        AssertConverts<double, short>(doubleConverter, -2, -2d);
        AssertConverts<double, ushort>(doubleConverter, 2, 2d);
        AssertConverts<double, int>(doubleConverter, -3, -3d);
        AssertConverts<double, uint>(doubleConverter, 3U, 3d);
        AssertConverts<double, long>(doubleConverter, -4L, -4d);
        AssertConverts<double, ulong>(doubleConverter, 4UL, 4d);
        AssertConverts<double, string>(doubleConverter, "867.53", 867.53d);
        AssertConverts<double, string>(doubleConverter, "867,53", 86_753d);
        AssertConverts<double, object>(doubleConverter, "42.5", 42.5d);
        AssertDoesNotConvert<double, object>(doubleConverter, null);
        AssertDoesNotConvert<double, object>(doubleConverter, "invalid");
        AssertDoesNotConvert<double, object>(doubleConverter, new object());
        AssertConverts<string, double>(doubleConverter, -42.5d, "-42.5");
    }

    static void AssertConverts<TResult, TInput>(ITypeConverter<TResult, TInput> converter, TInput input, TResult expected)
    {
        Assert.True(converter.TryConvert(input, out TResult? result));
        Assert.Equal(expected, result);
    }

    static void AssertDoesNotConvert<TResult, TInput>(ITypeConverter<TResult, TInput> converter, TInput? input)
    {
        Assert.False(converter.TryConvert(input, out TResult? result));
        Assert.Equal(default, result);
    }
}
