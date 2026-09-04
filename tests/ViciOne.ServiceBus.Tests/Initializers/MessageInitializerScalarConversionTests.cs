using System.Globalization;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class MessageInitializerScalarConversionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-CONVERSION", "value-type-to-string")]
    public async Task ValueTypeSource_UsesItsStringRepresentationAsync()
    {
        InitializeContext<StringMessage> context = await MessageInitializerCache<StringMessage>.InitializeAsync(new { Text = 1_234_567 }, TestContext.Current.CancellationToken);

        Assert.Equal("1234567", context.Message.Text);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-CONVERSION", "exact-type-copy")]
    public async Task MatchingScalarTypes_AreCopiedWithoutConversionAsync()
    {
        ScalarValues source = CreateValues();

        InitializeContext<ScalarMessage> context = await MessageInitializerCache<ScalarMessage>.InitializeAsync(source, TestContext.Current.CancellationToken);

        AssertScalarValues(context.Message, source);
        Assert.Same(source.ObjectValue, context.Message.ObjectValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-CONVERSION", "nullable-to-nonnullable")]
    public async Task NullableScalarSources_AreUnwrappedIntoNonNullableTargetsAsync()
    {
        ScalarValues expected = CreateValues();
        var source = new NullableScalarValues
        {
            StringValue = expected.StringValue,
            BoolValue = expected.BoolValue,
            ByteValue = expected.ByteValue,
            ShortValue = expected.ShortValue,
            IntValue = expected.IntValue,
            LongValue = expected.LongValue,
            DoubleValue = expected.DoubleValue,
            DecimalValue = expected.DecimalValue,
            DateTimeValue = expected.DateTimeValue,
            DateTimeOffsetValue = expected.DateTimeOffsetValue,
            TimeSpanValue = expected.TimeSpanValue,
            DayValue = expected.DayValue,
        };

        InitializeContext<ScalarMessage> context = await MessageInitializerCache<ScalarMessage>.InitializeAsync(source, TestContext.Current.CancellationToken);

        AssertScalarValues(context.Message, expected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-CONVERSION", "string-to-scalar")]
    public async Task RoundTripStrings_AreConvertedToScalarTargetsAsync()
    {
        ScalarValues expected = CreateValues();
        var source = new StringScalarValues
        {
            StringValue = expected.StringValue,
            BoolValue = expected.BoolValue.ToString(),
            ByteValue = expected.ByteValue.ToString(CultureInfo.InvariantCulture),
            ShortValue = expected.ShortValue.ToString(CultureInfo.InvariantCulture),
            IntValue = expected.IntValue.ToString(CultureInfo.InvariantCulture),
            LongValue = expected.LongValue.ToString(CultureInfo.InvariantCulture),
            DoubleValue = expected.DoubleValue.ToString(CultureInfo.InvariantCulture),
            DecimalValue = expected.DecimalValue.ToString(CultureInfo.InvariantCulture),
            DateTimeValue = expected.DateTimeValue.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffsetValue = expected.DateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture),
            TimeSpanValue = expected.TimeSpanValue.ToString("c", CultureInfo.InvariantCulture),
            DayValue = expected.DayValue.ToString(),
            ObjectValue = expected.ObjectValue,
        };

        InitializeContext<ScalarMessage> context = await MessageInitializerCache<ScalarMessage>.InitializeAsync(source, TestContext.Current.CancellationToken);

        AssertScalarValues(context.Message, expected);
        Assert.Same(expected.ObjectValue, context.Message.ObjectValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-CONVERSION", "nonnullable-to-nullable")]
    public async Task NonNullableScalarSources_AreWrappedByNullableTargetsAsync()
    {
        ScalarValues source = CreateValues();

        InitializeContext<NullableScalarMessage> context =
            await MessageInitializerCache<NullableScalarMessage>.InitializeAsync(source, TestContext.Current.CancellationToken);

        Assert.Equal(source.BoolValue, context.Message.BoolValue);
        Assert.Equal(source.ByteValue, context.Message.ByteValue);
        Assert.Equal(source.ShortValue, context.Message.ShortValue);
        Assert.Equal(source.IntValue, context.Message.IntValue);
        Assert.Equal(source.LongValue, context.Message.LongValue);
        Assert.Equal(source.DoubleValue, context.Message.DoubleValue);
        Assert.Equal(source.DecimalValue, context.Message.DecimalValue);
        Assert.Equal(source.DateTimeValue, context.Message.DateTimeValue);
        Assert.Equal(source.DateTimeOffsetValue, context.Message.DateTimeOffsetValue);
        Assert.Equal(source.TimeSpanValue, context.Message.TimeSpanValue);
        Assert.Equal(source.DayValue, context.Message.DayValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-SCALAR-CONVERSION", "chained-numeric-enum-and-uri")]
    public async Task ChainedContracts_ConvertNumericEnumAndUriValuesWithoutLosingMeaningAsync()
    {
        var timestamp = new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        var serviceAddress = new Uri("https://service.example.test/");
        var otherAddress = new Uri("https://other.example.test/");
        var stringAddress = new Uri("loopback://localhost/");
        var source = new
        {
            DateTimeValue = timestamp,
            Amount = 867.53m,
            EngineStatus = ConversionStatus.Started,
            NumberStatus = 12,
            StringStatus = "Started",
            IntValue = 27,
            NotNullableValue = (int?)69,
            NullableDecimalValue = 123.45m,
            NullableValue = 42,
            StringValue = "Hello",
            ServiceAddress = serviceAddress,
            OtherAddress = otherAddress.AbsoluteUri,
            StringAddress = stringAddress,
        };

        InitializeContext<ScalarIntermediateMessage> intermediate =
            await MessageInitializerCache<ScalarIntermediateMessage>.InitializeAsync(
                source,
                TestContext.Current.CancellationToken);
        InitializeContext<ChainedScalarMessage> result = await MessageInitializerCache<ChainedScalarMessage>.InitializeAsync(
            intermediate.Message,
            TestContext.Current.CancellationToken);

        Assert.Equal(timestamp, result.Message.DateTimeValue);
        Assert.Equal(867.53m, result.Message.Amount);
        Assert.Equal(ConversionStatus.Started, result.Message.EngineStatus);
        Assert.Equal(ConversionStatus.Stopped, result.Message.NumberStatus);
        Assert.Equal(ConversionStatus.Started, result.Message.StringStatus);
        Assert.Equal(27, result.Message.IntValue);
        Assert.Equal(69L, result.Message.NotNullableValue);
        Assert.Equal(123.45m, result.Message.NullableDecimalValue);
        Assert.Equal(42L, result.Message.NullableValue);
        Assert.Equal("Hello", result.Message.StringValue);
        Assert.Equal(serviceAddress, result.Message.ServiceAddress);
        Assert.Equal(otherAddress, result.Message.OtherAddress);
        Assert.Equal(stringAddress.AbsoluteUri, result.Message.StringAddress);
    }

    private static ScalarValues CreateValues() => new()
    {
        StringValue = "Hello",
        BoolValue = true,
        ByteValue = 123,
        ShortValue = 12_345,
        IntValue = 1_234_567,
        LongValue = 12_345_678L,
        DoubleValue = 867.5309,
        DecimalValue = 123.45m,
        DateTimeValue = new DateTime(2001, 2, 3, 4, 5, 6, 7, DateTimeKind.Utc),
        DateTimeOffsetValue = new DateTimeOffset(2001, 1, 2, 3, 4, 5, 6, TimeSpan.FromHours(-8)),
        TimeSpanValue = new TimeSpan(427, 1, 2, 3, 4),
        DayValue = Day.Tuesday,
        ObjectValue = new Uri("loopback://localhost/"),
    };

    private static void AssertScalarValues(ScalarMessage actual, ScalarValues expected)
    {
        Assert.Equal(expected.StringValue, actual.StringValue);
        Assert.Equal(expected.BoolValue, actual.BoolValue);
        Assert.Equal(expected.ByteValue, actual.ByteValue);
        Assert.Equal(expected.ShortValue, actual.ShortValue);
        Assert.Equal(expected.IntValue, actual.IntValue);
        Assert.Equal(expected.LongValue, actual.LongValue);
        Assert.Equal(expected.DoubleValue, actual.DoubleValue);
        Assert.Equal(expected.DecimalValue, actual.DecimalValue);
        Assert.Equal(expected.DateTimeValue, actual.DateTimeValue);
        Assert.Equal(expected.DateTimeOffsetValue, actual.DateTimeOffsetValue);
        Assert.Equal(expected.TimeSpanValue, actual.TimeSpanValue);
        Assert.Equal(expected.DayValue, actual.DayValue);
    }

    private sealed class ScalarValues
    {
        public required string StringValue { get; init; }

        public bool BoolValue { get; init; }

        public byte ByteValue { get; init; }

        public short ShortValue { get; init; }

        public int IntValue { get; init; }

        public long LongValue { get; init; }

        public double DoubleValue { get; init; }

        public decimal DecimalValue { get; init; }

        public DateTime DateTimeValue { get; init; }

        public DateTimeOffset DateTimeOffsetValue { get; init; }

        public TimeSpan TimeSpanValue { get; init; }

        public Day DayValue { get; init; }

        public required object ObjectValue { get; init; }
    }

    private sealed class NullableScalarValues
    {
        public required string StringValue { get; init; }

        public bool? BoolValue { get; init; }

        public byte? ByteValue { get; init; }

        public short? ShortValue { get; init; }

        public int? IntValue { get; init; }

        public long? LongValue { get; init; }

        public double? DoubleValue { get; init; }

        public decimal? DecimalValue { get; init; }

        public DateTime? DateTimeValue { get; init; }

        public DateTimeOffset? DateTimeOffsetValue { get; init; }

        public TimeSpan? TimeSpanValue { get; init; }

        public Day? DayValue { get; init; }
    }

    private sealed class StringScalarValues
    {
        public required string StringValue { get; init; }

        public required string BoolValue { get; init; }

        public required string ByteValue { get; init; }

        public required string ShortValue { get; init; }

        public required string IntValue { get; init; }

        public required string LongValue { get; init; }

        public required string DoubleValue { get; init; }

        public required string DecimalValue { get; init; }

        public required string DateTimeValue { get; init; }

        public required string DateTimeOffsetValue { get; init; }

        public required string TimeSpanValue { get; init; }

        public required string DayValue { get; init; }

        public required object ObjectValue { get; init; }
    }

    public enum Day
    {
        Sunday,
        Monday,
        Tuesday,
    }

    public enum ConversionStatus
    {
        Unknown = 0,
        Started = 1,
        Stopped = 12,
    }

    public interface ScalarIntermediateMessage
    {
        DateTime DateTimeValue { get; }

        string Amount { get; }

        ConversionStatus EngineStatus { get; }

        ConversionStatus NumberStatus { get; }

        ConversionStatus StringStatus { get; }

        int IntValue { get; }

        long NotNullableValue { get; }

        decimal? NullableDecimalValue { get; }

        long? NullableValue { get; }

        string StringValue { get; }

        Uri ServiceAddress { get; }

        Uri OtherAddress { get; }

        string StringAddress { get; }
    }

    public interface ChainedScalarMessage
    {
        DateTime DateTimeValue { get; }

        decimal Amount { get; }

        ConversionStatus EngineStatus { get; }

        ConversionStatus NumberStatus { get; }

        ConversionStatus StringStatus { get; }

        int IntValue { get; }

        long NotNullableValue { get; }

        decimal? NullableDecimalValue { get; }

        long? NullableValue { get; }

        string StringValue { get; }

        Uri ServiceAddress { get; }

        Uri OtherAddress { get; }

        string StringAddress { get; }
    }

    public interface StringMessage
    {
        string Text { get; }
    }

    public interface ScalarMessage
    {
        string StringValue { get; }

        bool BoolValue { get; }

        byte ByteValue { get; }

        short ShortValue { get; }

        int IntValue { get; }

        long LongValue { get; }

        double DoubleValue { get; }

        decimal DecimalValue { get; }

        DateTime DateTimeValue { get; }

        DateTimeOffset DateTimeOffsetValue { get; }

        TimeSpan TimeSpanValue { get; }

        Day DayValue { get; }

        object ObjectValue { get; }
    }

    public interface NullableScalarMessage
    {
        bool? BoolValue { get; }

        byte? ByteValue { get; }

        short? ShortValue { get; }

        int? IntValue { get; }

        long? LongValue { get; }

        double? DoubleValue { get; }

        decimal? DecimalValue { get; }

        DateTime? DateTimeValue { get; }

        DateTimeOffset? DateTimeOffsetValue { get; }

        TimeSpan? TimeSpanValue { get; }

        Day? DayValue { get; }
    }
}
