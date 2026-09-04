using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.DynamoDb.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDb.LocalIntegration.Tests.DynamoDbIntegration.Saga;

public sealed class DynamoDbSagaConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-CONFIGURATION", "frozen-sdk-options-reach-load-and-target-table-operations")]
    public async Task FrozenSdkOptions_ReachLoadAndTargetTableOperationsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("FrozenOptions", cancellationToken);
        var options = new DynamoDbSagaRepositoryOptions<ConfiguredSaga>(
            fixture.TableName,
            expiration: null,
            TimeProvider.System,
            consistentRead: true,
            isEmptyStringValueEnabled: true,
            retrieveDateTimeInUtc: true,
            conversion: DynamoDBEntryConversion.V2);

        LoadConfig mutableLoad = options.CreateLoadConfig();
        mutableLoad.OverrideTableName = "foreign-table";
        mutableLoad.ConsistentRead = false;
        GetTargetTableConfig mutableTarget = options.CreateTargetTableConfig();
        mutableTarget.OverrideTableName = "foreign-target";

        Guid sagaId = Guid.NewGuid();
        using var context = new DynamoDbDatabaseContext<ConfiguredSaga>(fixture.CreateContext(), options);
        await context.InsertAsync(new ConfiguredSaga { CorrelationId = sagaId, Value = "persisted" }, cancellationToken);
        ConfiguredSaga loaded = Assert.IsType<ConfiguredSaga>(await context.LoadAsync(sagaId, cancellationToken));

        LoadConfig effectiveLoad = options.CreateLoadConfig();
        GetTargetTableConfig effectiveTarget = options.CreateTargetTableConfig();
        Assert.Equal(fixture.TableName, effectiveLoad.OverrideTableName);
        Assert.Equal(fixture.TableName, effectiveTarget.OverrideTableName);
        Assert.True(effectiveLoad.ConsistentRead);
        Assert.True(effectiveLoad.IsEmptyStringValueEnabled);
        Assert.True(effectiveLoad.RetrieveDateTimeInUtc);
        Assert.Same(DynamoDBEntryConversion.V2, effectiveLoad.Conversion);
        Assert.Same(DynamoDBEntryConversion.V2, effectiveTarget.Conversion);
        Assert.Equal(sagaId, loaded.CorrelationId);
        Assert.Equal("persisted", loaded.Value);
        Assert.Single(await fixture.ScanAsync(cancellationToken));
    }

    public sealed class ConfiguredSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }

        public int Version { get; set; }

        public string Value { get; set; } = string.Empty;
    }
}
