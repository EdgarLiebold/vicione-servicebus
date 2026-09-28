using global::Azure.Data.Tables;
using ViciOne.ServiceBus.Azure.Table.Infrastructure;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.Tests.Saga;

public sealed class AzureTableEntityConverterTests
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
        IAzureTableEntityConverter<ConversionProbe> converter = AzureTableEntityConverterFactory.CreateConverter<ConversionProbe>();

        IDictionary<string, object> entity = converter.GetDictionary(expected);
        ConversionProbe actual = converter.GetObject(entity);

        Assert.IsType<bool>(entity[Stored(nameof(ConversionProbe.Enabled))]);
        Assert.IsType<int>(entity[Stored(nameof(ConversionProbe.Count))]);
        Assert.IsType<long>(entity[Stored(nameof(ConversionProbe.Sequence))]);
        Assert.IsType<double>(entity[Stored(nameof(ConversionProbe.Ratio))]);
        Assert.IsType<Guid>(entity[Stored(nameof(ConversionProbe.NativeId))]);
        Assert.IsType<DateTime>(entity[Stored(nameof(ConversionProbe.OccurredAt))]);
        Assert.IsType<DateTimeOffset>(entity[Stored(nameof(ConversionProbe.Offset))]);
        Assert.IsType<byte[]>(entity[Stored(nameof(ConversionProbe.Payload))]);
        Assert.IsType<string>(entity[Stored(nameof(ConversionProbe.Duration))]);
        Assert.IsType<string>(entity[Stored(nameof(ConversionProbe.Location))]);
        Assert.IsType<string>(entity[Stored(nameof(ConversionProbe.Version))]);
        Assert.IsType<string>(entity[Stored(nameof(ConversionProbe.State))]);
        Assert.IsType<string>(entity[Stored(nameof(ConversionProbe.Amount))]);
        Assert.IsType<string>(entity[Stored(nameof(ConversionProbe.Detail))]);
        Assert.DoesNotContain(Stored(nameof(ConversionProbe.OptionalText)), entity.Keys);
        Assert.All(entity.Keys, key => Assert.StartsWith(AzureTableEntityConverterFactory.SagaPropertyPrefix, key, StringComparison.Ordinal));

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

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION", "t78-sdk-offset-values-restore-utc-date-times-and-preserve-offsets")]
    public void EntityConverter_RestoresSdkOffsetValuesAsExactUtcDateTimesAndPreservesOffsetProperties()
    {
        DateTimeOffset requiredDate = new(2030, 3, 4, 5, 6, 7, TimeSpan.FromHours(5.5));
        DateTimeOffset optionalDate = new(2030, 3, 5, 6, 7, 8, TimeSpan.FromHours(-4));
        DateTimeOffset requiredOffset = new(2030, 4, 6, 7, 8, 9, TimeSpan.FromHours(3));
        DateTimeOffset optionalOffset = new(2030, 4, 7, 8, 9, 10, TimeSpan.FromHours(-7));
        var table = new TableEntity(new Dictionary<string, object>
        {
            [Stored(nameof(ConversionProbe.OccurredAt))] = requiredDate,
            [Stored(nameof(ConversionProbe.OptionalOccurredAt))] = optionalDate,
            [Stored(nameof(ConversionProbe.Offset))] = requiredOffset,
            [Stored(nameof(ConversionProbe.OptionalOffset))] = optionalOffset,
        });
        IAzureTableEntityConverter<ConversionProbe> converter = AzureTableEntityConverterFactory.CreateConverter<ConversionProbe>();

        ConversionProbe restored = converter.GetObject(table);

        Assert.Equal(requiredDate.UtcDateTime, restored.OccurredAt);
        Assert.Equal(DateTimeKind.Utc, restored.OccurredAt.Kind);
        Assert.Equal(optionalDate.UtcDateTime, restored.OptionalOccurredAt);
        Assert.Equal(DateTimeKind.Utc, restored.OptionalOccurredAt!.Value.Kind);
        Assert.Equal(requiredOffset, restored.Offset);
        Assert.Equal(requiredOffset.Offset, restored.Offset.Offset);
        Assert.Equal(optionalOffset, restored.OptionalOffset);
        Assert.Equal(optionalOffset.Offset, restored.OptionalOffset!.Value.Offset);
    }

    [Theory]
    [InlineData(nameof(ConversionProbe.Enabled), "true")]
    [InlineData(nameof(ConversionProbe.OptionalEnabled), "false")]
    [InlineData(nameof(ConversionProbe.Count), 42L)]
    [InlineData(nameof(ConversionProbe.OptionalCount), 42L)]
    [InlineData(nameof(ConversionProbe.Sequence), 42)]
    [InlineData(nameof(ConversionProbe.OptionalSequence), 42)]
    [InlineData(nameof(ConversionProbe.Ratio), 42)]
    [InlineData(nameof(ConversionProbe.OptionalRatio), 42)]
    [InlineData(nameof(ConversionProbe.NativeId), "018cc251-f400-7000-8000-000000000102")]
    [InlineData(nameof(ConversionProbe.OptionalNativeId), "018cc251-f400-7000-8000-000000000103")]
    [InlineData(nameof(ConversionProbe.OccurredAt), "2030-01-02T03:04:05Z")]
    [InlineData(nameof(ConversionProbe.OptionalOccurredAt), "2030-01-02T03:04:05Z")]
    [InlineData(nameof(ConversionProbe.Duration), 42)]
    [InlineData(nameof(ConversionProbe.OptionalDuration), 42)]
    [InlineData(nameof(ConversionProbe.Offset), "2030-01-02T03:04:05+02:00")]
    [InlineData(nameof(ConversionProbe.OptionalOffset), "2030-01-02T03:04:05+02:00")]
    [InlineData(nameof(ConversionProbe.Payload), "AQID")]
    [InlineData(nameof(ConversionProbe.Location), 42)]
    [InlineData(nameof(ConversionProbe.Version), 42)]
    [InlineData(nameof(ConversionProbe.Text), 42)]
    [InlineData(nameof(ConversionProbe.OptionalText), 42)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION", "t78-corrupt-native-fields-fail-closed-with-field-and-type")]
    public void EntityConverter_RejectsWrongNativeStorageTypesWithoutCoercingOrDefaulting(string propertyName, object wrongValue)
    {
        IAzureTableEntityConverter<ConversionProbe> converter = AzureTableEntityConverterFactory.CreateConverter<ConversionProbe>();
        var table = new TableEntity(new Dictionary<string, object>
        {
            [Stored(nameof(ConversionProbe.CorrelationId))] = Guid.Parse("018cc251-f400-7000-8000-000000000104"),
            [Stored(propertyName)] = wrongValue,
        });

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => converter.GetObject(table));

        Assert.Contains(Stored(propertyName), failure.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(ConversionProbe).GetProperty(propertyName)!.PropertyType.ToString(),
            failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION", "t78-sparse-null-versus-explicit-false-zero-and-empty")]
    public void EntityConverter_DistinguishesAbsentNullableValuesFromPersistedFalseZeroAndEmptyValues()
    {
        IAzureTableEntityConverter<SparseNativeProbe> converter = AzureTableEntityConverterFactory.CreateConverter<SparseNativeProbe>();
        Assert.Empty(converter.GetDictionary(new SparseNativeProbe()));
        var expected = new SparseNativeProbe
        {
            Enabled = false,
            Count = 0,
            Duration = TimeSpan.Zero,
            Payload = [],
            Text = string.Empty,
        };

        IDictionary<string, object> persisted = converter.GetDictionary(expected);
        var table = new TableEntity(persisted);
        SparseNativeProbe restored = converter.GetObject(table);

        Assert.Equal(5, persisted.Count);
        Assert.Equal(false, persisted[Stored(nameof(SparseNativeProbe.Enabled))]);
        Assert.Equal(0, persisted[Stored(nameof(SparseNativeProbe.Count))]);
        Assert.IsType<string>(persisted[Stored(nameof(SparseNativeProbe.Duration))]);
        Assert.Empty(Assert.IsType<byte[]>(persisted[Stored(nameof(SparseNativeProbe.Payload))]));
        Assert.Equal(string.Empty, persisted[Stored(nameof(SparseNativeProbe.Text))]);
        Assert.DoesNotContain(Stored(nameof(SparseNativeProbe.OptionalId)), persisted.Keys);
        Assert.DoesNotContain(Stored(nameof(SparseNativeProbe.OptionalDate)), persisted.Keys);
        Assert.Equal(false, restored.Enabled);
        Assert.Equal(0, restored.Count);
        Assert.Equal(TimeSpan.Zero, restored.Duration);
        Assert.Empty(Assert.IsType<byte[]>(restored.Payload));
        Assert.Equal(string.Empty, restored.Text);
        Assert.Null(restored.OptionalId);
        Assert.Null(restored.OptionalDate);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION", "saga-properties-cannot-collide-with-table-system-properties")]
    public void EntityConverter_IsolatesSagaPropertiesFromAzureTableSystemProperties()
    {
        var expected = new ReservedNameProbe
        {
            PartitionKey = "domain-partition",
            RowKey = "domain-row",
            Timestamp = "domain-timestamp",
            ETag = "domain-etag",
        };
        IAzureTableEntityConverter<ReservedNameProbe> converter =
            AzureTableEntityConverterFactory.CreateConverter<ReservedNameProbe>();

        IDictionary<string, object> persistedProperties = converter.GetDictionary(expected);
        var tableEntity = new TableEntity(persistedProperties)
        {
            PartitionKey = "storage-partition",
            RowKey = "storage-row",
        };
        ReservedNameProbe actual = converter.GetObject(tableEntity);

        Assert.Equal("domain-partition", persistedProperties[Stored(nameof(ReservedNameProbe.PartitionKey))]);
        Assert.Equal("domain-row", persistedProperties[Stored(nameof(ReservedNameProbe.RowKey))]);
        Assert.Equal("domain-timestamp", persistedProperties[Stored(nameof(ReservedNameProbe.Timestamp))]);
        Assert.Equal("domain-etag", persistedProperties[Stored(nameof(ReservedNameProbe.ETag))]);
        Assert.Equal(expected.PartitionKey, actual.PartitionKey);
        Assert.Equal(expected.RowKey, actual.RowKey);
        Assert.Equal(expected.Timestamp, actual.Timestamp);
        Assert.Equal(expected.ETag, actual.ETag);
    }

    [Theory]
    [InlineData(nameof(ConversionProbe.Duration), "not-a-duration")]
    [InlineData(nameof(ConversionProbe.Location), "http://[invalid")]
    [InlineData(nameof(ConversionProbe.Version), "not-a-version")]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION", "malformed-persisted-values-fail-closed")]
    public void EntityConverter_RejectsMalformedPersistedValues(string propertyName, string persistedValue)
    {
        IAzureTableEntityConverter<ConversionProbe> converter = AzureTableEntityConverterFactory.CreateConverter<ConversionProbe>();
        var entity = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Stored(propertyName)] = persistedValue,
        };

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => converter.GetObject(entity));

        Assert.Contains(propertyName, failure.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(ConversionProbe).GetProperty(propertyName)!.PropertyType.ToString(), failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION", "converter-inputs-fail-fast")]
    public void EntityConverters_RejectEveryMissingInputAtTheOwnedBoundary()
    {
        IAzureTableEntityConverter<ConversionProbe> converter = AzureTableEntityConverterFactory.CreateConverter<ConversionProbe>();
        var entity = new ConversionProbe();
        var properties = new Dictionary<string, object>(StringComparer.Ordinal);
        var native = new AzureTableSagaPropertyConverter<ConversionProbe, int>(
            nameof(ConversionProbe.Count),
            Stored(nameof(ConversionProbe.Count)));
        var value = new SerializedValueAzureTableSagaPropertyConverter<ConversionProbe, decimal>(
            nameof(ConversionProbe.Amount),
            Stored(nameof(ConversionProbe.Amount)));
        var reference = new SerializedReferenceAzureTableSagaPropertyConverter<ConversionProbe, ConversionDetail>(
            nameof(ConversionProbe.Detail),
            Stored(nameof(ConversionProbe.Detail)));

        Assert.Equal("converters", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableEntityConverter<ConversionProbe>(null!)).ParamName);
        Assert.Equal("converters", Assert.Throws<ArgumentException>(() =>
            new AzureTableEntityConverter<ConversionProbe>([null!])).ParamName);
        Assert.Equal("converters", Assert.Throws<ArgumentException>(() =>
            new AzureTableEntityConverter<ConversionProbe>([native, native])).ParamName);
        IAzureTableSagaPropertyConverter<ConversionProbe>[] tooManyConverters = Enumerable.Range(
                0,
                AzureTableStorageLimits.MaximumCustomPropertyCount + 1)
            .Select(index => (IAzureTableSagaPropertyConverter<ConversionProbe>)
                new AzureTableSagaPropertyConverter<ConversionProbe, int>(
                    nameof(ConversionProbe.Count),
                    $"Saga_Count{index}"))
            .ToArray();
        Assert.Equal("converters", Assert.Throws<ArgumentException>(() =>
            new AzureTableEntityConverter<ConversionProbe>(tooManyConverters)).ParamName);
        var mutableConverters = new List<IAzureTableSagaPropertyConverter<ConversionProbe>> { native };
        var defensivelyCopiedConverter = new AzureTableEntityConverter<ConversionProbe>(mutableConverters);
        mutableConverters.Clear();
        Assert.Equal(
            7,
            defensivelyCopiedConverter.GetDictionary(new ConversionProbe
            {
                Count = 7,
                OccurredAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Offset = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
            })[Stored(nameof(ConversionProbe.Count))]);
        InvalidOperationException materializationFailure = Assert.Throws<InvalidOperationException>(
            AzureTableEntityConverterFactory.CreateConverter<NoDefaultConstructorProbe>);
        Assert.Contains(typeof(NoDefaultConstructorProbe).FullName!, materializationFailure.Message, StringComparison.Ordinal);
        Assert.Contains("public parameterless constructor", materializationFailure.Message, StringComparison.Ordinal);
        Assert.Equal("entity", Assert.Throws<ArgumentNullException>(() =>
            converter.GetDictionary(null!)).ParamName);
        Assert.Equal("entityProperties", Assert.Throws<ArgumentNullException>(() =>
            converter.GetObject(null!)).ParamName);
        Assert.Equal("propertyName", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaPropertyConverter<ConversionProbe, int>(null!, "Saga_Count")).ParamName);
        Assert.Equal("storageName", Assert.Throws<ArgumentException>(() =>
            new AzureTableSagaPropertyConverter<ConversionProbe, int>(nameof(ConversionProbe.Count), " ")).ParamName);
        Assert.Equal("storageName", Assert.Throws<ArgumentException>(() =>
            new AzureTableSagaPropertyConverter<ConversionProbe, int>(
                nameof(ConversionProbe.Count),
                new string('x', AzureTableStorageLimits.MaximumPropertyNameCharacters + 1))).ParamName);
        Assert.Equal("propertyName", Assert.Throws<ArgumentException>(() =>
            new SerializedValueAzureTableSagaPropertyConverter<ConversionProbe, decimal>(" ", "Saga_Amount")).ParamName);
        Assert.Equal("storageName", Assert.Throws<ArgumentNullException>(() =>
            new SerializedValueAzureTableSagaPropertyConverter<ConversionProbe, decimal>(nameof(ConversionProbe.Amount), null!)).ParamName);
        Assert.Equal("propertyName", Assert.Throws<ArgumentNullException>(() =>
            new SerializedReferenceAzureTableSagaPropertyConverter<ConversionProbe, ConversionDetail>(null!, "Saga_Detail")).ParamName);
        Assert.Equal("storageName", Assert.Throws<ArgumentException>(() =>
            new SerializedReferenceAzureTableSagaPropertyConverter<ConversionProbe, ConversionDetail>(nameof(ConversionProbe.Detail), " ")).ParamName);

        Assert.Equal("entity", Assert.Throws<ArgumentNullException>(() =>
            native.ToEntity(null!, properties)).ParamName);
        Assert.Equal("entityProperties", Assert.Throws<ArgumentNullException>(() =>
            native.ToEntity(entity, null!)).ParamName);
        Assert.Equal("entity", Assert.Throws<ArgumentNullException>(() =>
            value.FromEntity(null!, properties)).ParamName);
        Assert.Equal("entityProperties", Assert.Throws<ArgumentNullException>(() =>
            reference.FromEntity(entity, null!)).ParamName);

        var wrongSerializedValue = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Stored(nameof(ConversionProbe.Amount))] = 42,
        };
        InvalidOperationException valueFailure = Assert.Throws<InvalidOperationException>(() =>
            converter.GetObject(wrongSerializedValue));
        Assert.Contains(nameof(ConversionProbe.Amount), valueFailure.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(decimal).ToString(), valueFailure.Message, StringComparison.Ordinal);

        wrongSerializedValue[Stored(nameof(ConversionProbe.Amount))] = "null";
        InvalidOperationException missingValueFailure = Assert.Throws<InvalidOperationException>(() =>
            converter.GetObject(wrongSerializedValue));
        Assert.Contains(nameof(ConversionProbe.Amount), missingValueFailure.Message, StringComparison.Ordinal);
        Assert.Contains("contains no value", missingValueFailure.Message, StringComparison.Ordinal);

        var wrongSerializedReference = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Stored(nameof(ConversionProbe.Detail))] = 42,
        };
        InvalidOperationException referenceFailure = Assert.Throws<InvalidOperationException>(() =>
            converter.GetObject(wrongSerializedReference));
        Assert.Contains(nameof(ConversionProbe.Detail), referenceFailure.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(ConversionDetail).ToString(), referenceFailure.Message, StringComparison.Ordinal);

        var wrongCase = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["saga_Count"] = 42,
        };
        Assert.Equal(0, converter.GetObject(wrongCase).Count);

        InvalidOperationException stringSizeFailure = Assert.Throws<InvalidOperationException>(() =>
            converter.GetDictionary(new ConversionProbe
            {
                OccurredAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Offset = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Text = new string('x', 32_769),
            }));
        Assert.Contains(nameof(ConversionProbe.Text), stringSizeFailure.Message, StringComparison.Ordinal);
        Assert.Contains("UTF-16", stringSizeFailure.Message, StringComparison.Ordinal);

        InvalidOperationException binarySizeFailure = Assert.Throws<InvalidOperationException>(() =>
            converter.GetDictionary(new ConversionProbe
            {
                OccurredAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Offset = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Payload = new byte[AzureTableStorageLimits.MaximumPropertyBytes + 1],
            }));
        Assert.Contains(nameof(ConversionProbe.Payload), binarySizeFailure.Message, StringComparison.Ordinal);
        Assert.Contains("binary", binarySizeFailure.Message, StringComparison.Ordinal);

        InvalidOperationException dateTimeFailure = Assert.Throws<InvalidOperationException>(() =>
            converter.GetDictionary(new ConversionProbe
            {
                OccurredAt = new DateTime(1600, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            }));
        Assert.Contains(nameof(ConversionProbe.OccurredAt), dateTimeFailure.Message, StringComparison.Ordinal);
        Assert.Contains("1601-01-01", dateTimeFailure.Message, StringComparison.Ordinal);
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

    public sealed class SparseNativeProbe
    {
        public bool? Enabled { get; set; }
        public int? Count { get; set; }
        public TimeSpan? Duration { get; set; }
        public byte[]? Payload { get; set; }
        public string? Text { get; set; }
        public Guid? OptionalId { get; set; }
        public DateTime? OptionalDate { get; set; }
    }

    public sealed class ReservedNameProbe
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string ETag { get; set; } = string.Empty;
    }

    public sealed class NoDefaultConstructorProbe(string value)
    {
        public string Value { get; set; } = value;
    }

    public enum ConversionState
    {
        Initial,
        Active,
    }

    private static string Stored(string propertyName) =>
        $"{AzureTableEntityConverterFactory.SagaPropertyPrefix}{propertyName}";
}
