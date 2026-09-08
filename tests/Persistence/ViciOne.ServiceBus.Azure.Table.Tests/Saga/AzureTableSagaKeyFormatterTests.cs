using global::Azure.Data.Tables;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.Tests.Saga;

public sealed class AzureTableSagaKeyFormatterTests
{
    private static readonly Guid CorrelationId = Guid.Parse("018cc251-f400-7000-8000-000000000201");

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-KEY", "built-in-formatters-produce-exact-pairs")]
    public void BuiltInFormatters_ProduceExactStablePairs()
    {
        var partitionFormatter = new FixedPartitionSagaKeyFormatter("sagas");
        var rowFormatter = new FixedRowSagaKeyFormatter("state");

        (string partitionKey, string rowKey) partitionPair = partitionFormatter.Format(CorrelationId);
        (string partitionKey, string rowKey) rowPair = rowFormatter.Format(CorrelationId);

        Assert.Equal(("sagas", CorrelationId.ToString("D")), partitionPair);
        Assert.Equal((CorrelationId.ToString("D"), "state"), rowPair);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid/key")]
    [InlineData("invalid\\key")]
    [InlineData("invalid#key")]
    [InlineData("invalid?key")]
    [InlineData("invalid\u001fkey")]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-KEY", "invalid-constant-keys-rejected-before-use")]
    public void BuiltInFormatters_RejectInvalidConstantKeys(string invalidKey)
    {
        ArgumentException partitionFailure = Assert.ThrowsAny<ArgumentException>(
            () => new FixedPartitionSagaKeyFormatter(invalidKey));
        ArgumentException rowFailure = Assert.ThrowsAny<ArgumentException>(
            () => new FixedRowSagaKeyFormatter(invalidKey));

        Assert.Equal("partitionKey", partitionFailure.ParamName);
        Assert.Equal("rowKey", rowFailure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-KEY", "empty-correlation-id-rejected")]
    public void BuiltInFormatters_RejectAnEmptyCorrelationId()
    {
        var partitionFormatter = new FixedPartitionSagaKeyFormatter("sagas");
        var rowFormatter = new FixedRowSagaKeyFormatter("state");

        ArgumentException partitionFailure = Assert.Throws<ArgumentException>(
            () => partitionFormatter.Format(Guid.Empty));
        ArgumentException rowFailure = Assert.Throws<ArgumentException>(
            () => rowFormatter.Format(Guid.Empty));

        Assert.Equal("correlationId", partitionFailure.ParamName);
        Assert.Equal("correlationId", rowFailure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-KEY", "custom-formatter-output-validated-at-repository-boundary")]
    public void StorageContext_RejectsUnsafeCustomFormatterOutputBeforeNetworkUse()
    {
        var credential = new TableSharedKeyCredential("localaccount", Convert.ToBase64String(new byte[32]));
        var table = new TableClient(new Uri("http://127.0.0.1:1/localaccount"), "sagas", credential);
        var context = new AzureTableSagaStorageContext<KeyProbeSaga>(table, new UnsafeFormatter());

        ArgumentException failure = Assert.Throws<ArgumentException>(() => context.Format(CorrelationId));

        Assert.Equal("partitionKey", failure.ParamName);
    }

    public sealed class KeyProbeSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class UnsafeFormatter : IAzureTableSagaKeyFormatter
    {
        public (string partitionKey, string rowKey) Format(Guid correlationId) =>
            ("unsafe/partition", correlationId.ToString("D"));
    }
}
