using Amazon.DynamoDBv2.Model;
using ViciOne.ServiceBus.DynamoDb;
using ViciOne.ServiceBus.DynamoDb.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDb.LocalIntegration.Tests.Saga;

public sealed class DynamoDbSagaExpirationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-EXPIRATION", "each-write-uses-injected-clock-and-null-ttl-is-omitted")]
    public async Task EachWrite_UsesInjectedClockAndPersistsNumericEpochSecondsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("SagaExpiration", cancellationToken);
        var timeProvider = new ControlledTimeProvider(new DateTimeOffset(2032, 04, 05, 06, 07, 08, TimeSpan.Zero));
        var expiringOptions = new DynamoDbSagaRepositoryOptions<ExpiringSaga>(
            fixture.TableName,
            TimeSpan.FromMinutes(5),
            timeProvider);
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();
        Guid nonExpiringId = Guid.NewGuid();

        using (var writer = new DynamoDbSagaStore<ExpiringSaga>(fixture.CreateContext(), expiringOptions))
        {
            await writer.CreateAsync(new ExpiringSaga { CorrelationId = firstId }, cancellationToken);
            timeProvider.UtcNow = timeProvider.UtcNow.AddMinutes(2);
            await writer.CreateAsync(new ExpiringSaga { CorrelationId = secondId }, cancellationToken);
        }

        using (var writer = new DynamoDbSagaStore<ExpiringSaga>(
                   fixture.CreateContext(),
                   new DynamoDbSagaRepositoryOptions<ExpiringSaga>(fixture.TableName)))
        {
            await writer.CreateAsync(new ExpiringSaga { CorrelationId = nonExpiringId }, cancellationToken);
        }

        Dictionary<string, AttributeValue>[] rows = await fixture.ScanAsync(cancellationToken);
        Dictionary<string, AttributeValue> first = Assert.Single(rows, row => row["PK"].S == firstId.ToString("D"));
        Dictionary<string, AttributeValue> second = Assert.Single(rows, row => row["PK"].S == secondId.ToString("D"));
        Dictionary<string, AttributeValue> nonExpiring = Assert.Single(rows, row => row["PK"].S == nonExpiringId.ToString("D"));
        long expectedFirst = new DateTimeOffset(2032, 04, 05, 06, 12, 08, TimeSpan.Zero).ToUnixTimeSeconds();
        long expectedSecond = new DateTimeOffset(2032, 04, 05, 06, 14, 08, TimeSpan.Zero).ToUnixTimeSeconds();

        Assert.Equal(expectedFirst.ToString(System.Globalization.CultureInfo.InvariantCulture), first[nameof(DynamoDbSagaDocument.ExpirationEpochSeconds)].N);
        Assert.Equal(expectedSecond.ToString(System.Globalization.CultureInfo.InvariantCulture), second[nameof(DynamoDbSagaDocument.ExpirationEpochSeconds)].N);
        Assert.DoesNotContain(nameof(DynamoDbSagaDocument.ExpirationEpochSeconds), nonExpiring.Keys);
    }

    public sealed class ExpiringSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }

        public int Version { get; set; }
    }

    private sealed class ControlledTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
