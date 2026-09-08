using global::Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Saga;

public sealed class AzureTableSagaEntityConversionIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-ENTITY-CONVERSION", "native-serialized-and-reserved-properties-round-trip-through-real-table-api")]
    public async Task SagaProperties_RoundTripThroughTheRealAzureTableApiAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync(
            "SagaConversion",
            cancellationToken);
        var expected = new ProviderConversionSaga
        {
            CorrelationId = Guid.CreateVersion7(),
            Enabled = true,
            Count = 42,
            Sequence = long.MaxValue - 1,
            Ratio = 12.5,
            OccurredAt = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Offset = new DateTimeOffset(2030, 1, 2, 5, 4, 5, TimeSpan.FromHours(2)),
            Payload = [0, 1, 2, 254, 255],
            Duration = TimeSpan.FromMinutes(17) + TimeSpan.FromTicks(3),
            Location = new Uri("urn:vicione:azure-table:provider-conversion"),
            Version = new Version(10, 2, 3, 4),
            State = ConversionState.Active,
            Amount = 1234567890.123456789m,
            Detail = new ConversionDetail { Name = "nested", Value = 19 },
            PartitionKey = "domain-partition",
            RowKey = "domain-row",
            Timestamp = "domain-timestamp",
            ETag = "domain-etag",
        };
        var storage = new AzureTableSagaStorageContext<ProviderConversionSaga>(
            fixture.Table,
            new FixedPartitionSagaKeyFormatter(nameof(ProviderConversionSaga)));
        IDictionary<string, object> properties = storage.Converter.GetDictionary(expected);
        var entity = new TableEntity(properties);
        (entity.PartitionKey, entity.RowKey) = storage.Format(expected.CorrelationId);

        await fixture.Table.AddEntityAsync(entity, cancellationToken);
        TableEntity persisted = (await fixture.Table.GetEntityAsync<TableEntity>(
            entity.PartitionKey,
            entity.RowKey,
            cancellationToken: cancellationToken)).Value;
        ProviderConversionSaga actual = storage.Converter.GetObject(persisted);

        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.Enabled, actual.Enabled);
        Assert.Equal(expected.Count, actual.Count);
        Assert.Equal(expected.Sequence, actual.Sequence);
        Assert.Equal(expected.Ratio, actual.Ratio);
        Assert.Equal(expected.OccurredAt, actual.OccurredAt);
        Assert.Equal(expected.Offset, actual.Offset);
        Assert.Equal(expected.Payload, actual.Payload);
        Assert.Equal(expected.Duration, actual.Duration);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(expected.Amount, actual.Amount);
        Assert.Equal(expected.Detail.Name, actual.Detail.Name);
        Assert.Equal(expected.Detail.Value, actual.Detail.Value);
        Assert.Equal(expected.PartitionKey, actual.PartitionKey);
        Assert.Equal(expected.RowKey, actual.RowKey);
        Assert.Equal(expected.Timestamp, actual.Timestamp);
        Assert.Equal(expected.ETag, actual.ETag);
        Assert.DoesNotContain(properties.Keys, name => name is "PartitionKey" or "RowKey" or "Timestamp" or "ETag");
    }

    public sealed class ProviderConversionSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
        public bool Enabled { get; set; }
        public int Count { get; set; }
        public long Sequence { get; set; }
        public double Ratio { get; set; }
        public DateTime OccurredAt { get; set; }
        public DateTimeOffset Offset { get; set; }
        public byte[] Payload { get; set; } = [];
        public TimeSpan Duration { get; set; }
        public Uri Location { get; set; } = null!;
        public Version Version { get; set; } = null!;
        public ConversionState State { get; set; }
        public decimal Amount { get; set; }
        public ConversionDetail Detail { get; set; } = null!;
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string ETag { get; set; } = string.Empty;
    }

    public sealed class ConversionDetail
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public enum ConversionState
    {
        Initial,
        Active,
    }
}
