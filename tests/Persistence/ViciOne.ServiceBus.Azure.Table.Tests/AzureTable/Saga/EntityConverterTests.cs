using ViciOne.ServiceBus.AzureTable;
using ViciOne.ServiceBus.AzureTable.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.Tests.AzureTable.Saga;

public sealed class EntityConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION", "native-and-serialized-properties-round-trip")]
    public void EntityConverter_RoundTripsNativeAndSerializedPropertiesWithoutLoss()
    {
        var expected = new ConversionProbe
        {
            CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000101"),
            Enabled = true,
            OptionalEnabled = false,
            Count = 42,
            OptionalCount = 43,
            Sequence = 9_223_372_036_854_775_000,
            OptionalSequence = 9_223_372_036_854_774_999,
            Ratio = 12.5,
            OptionalRatio = -0.25,
            NativeId = Guid.Parse("018cc251-f400-7000-8000-000000000102"),
            OptionalNativeId = Guid.Parse("018cc251-f400-7000-8000-000000000103"),
            OccurredAt = new DateTime(2030, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc),
            OptionalOccurredAt = new DateTime(2030, 1, 3, 4, 5, 6, 789, DateTimeKind.Utc),
            Duration = TimeSpan.FromMinutes(17) + TimeSpan.FromTicks(3),
            OptionalDuration = TimeSpan.FromSeconds(9),
            Offset = new DateTimeOffset(2030, 2, 3, 4, 5, 6, TimeSpan.FromHours(2)),
            OptionalOffset = new DateTimeOffset(2030, 2, 4, 5, 6, 7, TimeSpan.Zero),
            Payload = [0, 1, 2, 254, 255],
            Location = new Uri("urn:vicione:azure-table:test"),
            Version = new Version(10, 2, 3, 4),
            Text = "persisted",
            State = ConversionState.Active,
            Amount = 1234567890.123456789m,
            Detail = new ConversionDetail { Name = "nested", Value = 19 },
        };
        IEntityConverter<ConversionProbe> converter = EntityConverterFactory.CreateConverter<ConversionProbe>();

        IDictionary<string, object> entity = converter.GetDictionary(expected);
        ConversionProbe actual = converter.GetObject(entity);

        Assert.IsType<bool>(entity[nameof(ConversionProbe.Enabled)]);
        Assert.IsType<int>(entity[nameof(ConversionProbe.Count)]);
        Assert.IsType<long>(entity[nameof(ConversionProbe.Sequence)]);
        Assert.IsType<double>(entity[nameof(ConversionProbe.Ratio)]);
        Assert.IsType<Guid>(entity[nameof(ConversionProbe.NativeId)]);
        Assert.IsType<DateTime>(entity[nameof(ConversionProbe.OccurredAt)]);
        Assert.IsType<DateTimeOffset>(entity[nameof(ConversionProbe.Offset)]);
        Assert.IsType<byte[]>(entity[nameof(ConversionProbe.Payload)]);
        Assert.IsType<string>(entity[nameof(ConversionProbe.Duration)]);
        Assert.IsType<string>(entity[nameof(ConversionProbe.Location)]);
        Assert.IsType<string>(entity[nameof(ConversionProbe.Version)]);
        Assert.IsType<string>(entity[nameof(ConversionProbe.State)]);
        Assert.IsType<string>(entity[nameof(ConversionProbe.Amount)]);
        Assert.IsType<string>(entity[nameof(ConversionProbe.Detail)]);
        Assert.DoesNotContain(nameof(ConversionProbe.OptionalText), entity.Keys);

        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.Enabled, actual.Enabled);
        Assert.Equal(expected.OptionalEnabled, actual.OptionalEnabled);
        Assert.Equal(expected.Count, actual.Count);
        Assert.Equal(expected.OptionalCount, actual.OptionalCount);
        Assert.Equal(expected.Sequence, actual.Sequence);
        Assert.Equal(expected.OptionalSequence, actual.OptionalSequence);
        Assert.Equal(expected.Ratio, actual.Ratio);
        Assert.Equal(expected.OptionalRatio, actual.OptionalRatio);
        Assert.Equal(expected.NativeId, actual.NativeId);
        Assert.Equal(expected.OptionalNativeId, actual.OptionalNativeId);
        Assert.Equal(expected.OccurredAt, actual.OccurredAt);
        Assert.Equal(expected.OptionalOccurredAt, actual.OptionalOccurredAt);
        Assert.Equal(expected.Duration, actual.Duration);
        Assert.Equal(expected.OptionalDuration, actual.OptionalDuration);
        Assert.Equal(expected.Offset, actual.Offset);
        Assert.Equal(expected.OptionalOffset, actual.OptionalOffset);
        Assert.Equal(expected.Payload, actual.Payload);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.Text, actual.Text);
        Assert.Null(actual.OptionalText);
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(expected.Amount, actual.Amount);
        Assert.Equal(expected.Detail.Name, actual.Detail.Name);
        Assert.Equal(expected.Detail.Value, actual.Detail.Value);
    }

    [Theory]
    [InlineData(nameof(ConversionProbe.Duration), "not-a-duration")]
    [InlineData(nameof(ConversionProbe.Location), "http://[invalid")]
    [InlineData(nameof(ConversionProbe.Version), "not-a-version")]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION", "malformed-persisted-values-fail-closed")]
    public void EntityConverter_RejectsMalformedPersistedValues(string propertyName, string persistedValue)
    {
        IEntityConverter<ConversionProbe> converter = EntityConverterFactory.CreateConverter<ConversionProbe>();
        var entity = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [propertyName] = persistedValue,
        };

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => converter.GetObject(entity));

        Assert.Contains(propertyName, failure.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(ConversionProbe).GetProperty(propertyName)!.PropertyType.ToString(), failure.Message, StringComparison.Ordinal);
    }

    public sealed class ConversionProbe
    {
        public Guid CorrelationId { get; set; }
        public bool Enabled { get; set; }
        public bool? OptionalEnabled { get; set; }
        public int Count { get; set; }
        public int? OptionalCount { get; set; }
        public long Sequence { get; set; }
        public long? OptionalSequence { get; set; }
        public double Ratio { get; set; }
        public double? OptionalRatio { get; set; }
        public Guid NativeId { get; set; }
        public Guid? OptionalNativeId { get; set; }
        public DateTime OccurredAt { get; set; }
        public DateTime? OptionalOccurredAt { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan? OptionalDuration { get; set; }
        public DateTimeOffset Offset { get; set; }
        public DateTimeOffset? OptionalOffset { get; set; }
        public byte[] Payload { get; set; } = [];
        public Uri Location { get; set; } = null!;
        public Version Version { get; set; } = null!;
        public string Text { get; set; } = "";
        public string? OptionalText { get; set; }
        public ConversionState State { get; set; }
        public decimal Amount { get; set; }
        public ConversionDetail Detail { get; set; } = null!;
    }

    public sealed class ConversionDetail
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public enum ConversionState
    {
        Initial,
        Active,
    }
}
