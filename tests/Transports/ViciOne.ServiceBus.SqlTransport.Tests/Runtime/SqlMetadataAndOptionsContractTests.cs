using System.Text.Json;
using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlMetadataAndOptionsContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SQL-HEADER-ENUMERATION", "native-metadata-agrees-with-lookup-despite-serialized-spoof")]
    public void HeaderEnumeration_PreservesAuthoritativeNativeValuesAndExplicitRedeliveryOverride(bool alternateNativeKeyCase)
    {
        Guid messageId = Guid.Parse("d738c194-7fb4-49d7-b4b6-737b7d6b4e21");
        string serializedMessageIdKey = alternateNativeKeyCase
            ? MessageHeaders.MessageId.ToLowerInvariant()
            : MessageHeaders.MessageId;
        Guid transportId = Guid.Parse("b82bb128-bafb-418a-a035-ea0d072c771e");
        var message = new SqlTransportMessage
        {
            TransportMessageId = transportId, MessageId = messageId, DeliveryCount = 5,
            RoutingKey = "orders.é", PartitionKey = "租户-7",
            Headers = JsonSerializer.Serialize(new[]
            {
                new KeyValuePair<string, object>(serializedMessageIdKey, "application-spoof"),
                new KeyValuePair<string, object>("application-only", "retained"),
                new KeyValuePair<string, object>("shared", "application"),
            }),
            TransportHeaders = JsonSerializer.Serialize(new[]
            {
                new KeyValuePair<string, object>(serializedMessageIdKey, "transport-spoof"),
                new KeyValuePair<string, object>(MessageHeaders.RedeliveryCount, 0),
                new KeyValuePair<string, object>("shared", "transport"),
            }),
        };
        var provider = new SqlHeaderProvider(message);
        var expectedNative = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            [MessageHeaders.TransportMessageId] = transportId,
            [MessageHeaders.MessageId] = messageId,
            [nameof(SqlTransportMessage.DeliveryCount)] = 5,
            [MessageHeaders.RedeliveryCount] = 0,
            [nameof(SqlTransportMessage.RoutingKey)] = "orders.é",
            [nameof(SqlTransportMessage.PartitionKey)] = "租户-7",
        };
        KeyValuePair<string, object>[] enumerated = provider.GetAll().ToArray();
        Assert.Equal(enumerated.Length, enumerated.Select(header => header.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Dictionary<string, object> all = enumerated.ToDictionary(header => header.Key, header => header.Value,
            StringComparer.OrdinalIgnoreCase);

        foreach ((string key, object expected) in expectedNative)
        {
            Assert.True(provider.TryGetHeader(key, out object? found), key);
            Assert.Equal(expected, found);
            Assert.Equal(expected, Assert.Contains(key, all));
            Assert.Equal(expected.GetType(), all[key].GetType());
        }
        foreach (KeyValuePair<string, object> header in enumerated)
        {
            Assert.True(provider.TryGetHeader(header.Key, out object? lookup), header.Key);
            Assert.Equal(header.Value, lookup);
            Assert.Equal(header.Value.GetType(), lookup!.GetType());
        }
        Assert.Equal("retained", all["application-only"]);
        Assert.Equal("transport", all["shared"]);
        Assert.True(provider.TryGetHeader("SHARED", out object? shared));
        Assert.Equal("transport", shared);
        Assert.False(provider.TryGetHeader(MessageHeaders.CorrelationId, out _));
    }

    [Theory]
    [InlineData("connection-host,1544", "connection-host,2444")]
    [InlineData("connection-host\\named,1544", "connection-host\\named,2444")]
    [RequirementCoverage("REQ-VSB-SQLSERVER-PORT-PRECEDENCE", "explicit-port-alone-overrides-base-datasource-without-security-downgrade")]
    public void ConnectionOptions_ExplicitPortWithoutHostOverridesConnectionStringDataSource(
        string dataSource, string expected)
    {
        // Uses the existing friend-access unit seam; it does not open a database connection.
        var original = new SqlConnectionStringBuilder
        {
            DataSource = dataSource, InitialCatalog = "transport_tests", UserID = "test_user",
            Password = "test_password", TrustServerCertificate = false,
        };
        var options = new SqlTransportOptions { ConnectionString = original.ConnectionString, Port = 2444 };
        SqlConnectionStringBuilder projected = SqlServerTransportConnection.CreateBuilder(options);
        SqlConnectionStringBuilder control = SqlServerTransportConnection.CreateBuilder(
            new SqlTransportOptions { ConnectionString = original.ConnectionString });

        Assert.Equal(expected, projected.DataSource);
        Assert.Equal(dataSource, control.DataSource);
        Assert.Equal(original.InitialCatalog, projected.InitialCatalog);
        Assert.Equal(original.UserID, projected.UserID);
        Assert.Equal(original.Password, projected.Password);
        Assert.Equal(original.Encrypt, projected.Encrypt);
        Assert.False(projected.TrustServerCertificate);
        Assert.Null(options.Host);
        Assert.Equal(2444, options.Port);
        Assert.Equal(original.ConnectionString, options.ConnectionString);
    }
}
