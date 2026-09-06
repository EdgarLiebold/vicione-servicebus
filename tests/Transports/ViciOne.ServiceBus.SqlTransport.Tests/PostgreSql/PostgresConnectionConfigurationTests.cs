using System.Text;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.Helpers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.PostgreSql;

public sealed class PostgresConnectionConfigurationTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0022", "native-owner")]
    public void NotifyChannel_UsesStableNamespaceHashAndQueueId()
    {
        string actual = NotifyChannel.CreateName("transport", 42);

        Assert.Equal("vsb_6694ea8075001f6628da20f1afdafc74a7_msg_42", actual);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0023", "native-owner")]
    public void NotifyChannel_DistinguishesSchemasThatShareALongPrefix()
    {
        string commonPrefix = new('x', 1_000);

        string first = NotifyChannel.CreateName($"{commonPrefix}-first", 42);
        string second = NotifyChannel.CreateName($"{commonPrefix}-second", 42);

        Assert.NotEqual(first, second);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0024", "native-owner")]
    public void NotifyChannel_DefaultsMissingSchema()
    {
        string?[] schemas = [null, string.Empty, " \t"];
        string expected = NotifyChannel.CreateName("transport", 42);

        Assert.All(schemas, schema => Assert.Equal(expected, NotifyChannel.CreateName(schema, 42)));
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    [RequirementCoverage("REQ-VSB-POSTGRES-NOTIFY-CHANNEL", "utf8-identifier-budget")]
    public void NotifyChannel_UsesAtMostSixtyThreeBytesForUnicodeSchemaAndAnyQueueId(long queueId)
    {
        const string schema = "租户-🚚-äöü-very-long-schema-name-that-exceeds-the-original-character-budget";

        string actual = NotifyChannel.CreateName(schema, queueId);

        Assert.InRange(Encoding.UTF8.GetByteCount(actual), 1, 63);
        Assert.Matches("^[a-z0-9_-]+$", actual);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0026", "native-owner")]
    public async Task ConnectionOptions_ParseAndProjectAdminConnectionAndAzurePrincipalAsync()
    {
        string[] connectionStrings =
        [
            "Host=localhost;Username=admin;Password=test-password;Database=sample",
            "Host=messaging.postgres.database.azure.com;Username=admin@messaging;Password=test-password;Database=sample",
            "Host=messaging.server.com;Username=admin;Password=test-password;Database=sample",
        ];

        foreach (string connectionString in connectionStrings)
        {
            var source = new NpgsqlConnectionStringBuilder(connectionString);
            var options = new SqlTransportOptions { ConnectionString = connectionString };
            await using PostgresSqlTransportConnection connection =
                PostgresSqlTransportConnection.GetDatabaseAdminConnection(options);
            var projected = new NpgsqlConnectionStringBuilder(connection.Connection.ConnectionString);

            Assert.Equal("sample", projected.Database);
            Assert.Equal("test-password", projected.Password);
            Assert.Equal(source.Username, projected.Username);
            Assert.Equal(source.Host, projected.Host);
            Assert.Equal("admin", PostgresSqlTransportConnection.GetAdminMigrationPrincipal(options));
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0027", "native-owner")]
    public async Task HostSettings_PreserveMultipleHostsAndUseFirstBusHostAsync()
    {
        var settings = new PostgresSqlHostSettings(Options("local,remote"));
        await using NpgsqlDataSource dataSource = settings.GetDataSource();
        var builder = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Equal("local,remote", builder.Host);
        Assert.Equal("local", settings.HostAddress.Host);
        Assert.Equal(-1, settings.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0028", "native-owner")]
    public async Task HostSettings_ApplyExplicitPortToMultipleHostsAsync()
    {
        SqlTransportOptions options = Options("local,remote");
        options.Port = 1234;
        var settings = new PostgresSqlHostSettings(options);
        await using NpgsqlDataSource dataSource = settings.GetDataSource();
        var builder = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Equal("local,remote", builder.Host);
        Assert.Equal(1234, builder.Port);
        Assert.Equal(1234, settings.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0029", "native-owner")]
    public async Task HostSettings_PreservePerHostPortsAndProjectFirstHostAsync()
    {
        var settings = new PostgresSqlHostSettings(Options("local:1234,remote:5678"));
        await using NpgsqlDataSource dataSource = settings.GetDataSource();
        var builder = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Equal("local:1234,remote:5678", builder.Host);
        Assert.Equal("local", settings.HostAddress.Host);
        Assert.Equal(-1, settings.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0030", "native-owner")]
    public void HostSettings_SplitSingleColonQualifiedHostForBusAddress()
    {
        SqlTransportOptions options = Options("local:1234");
        NpgsqlConnectionStringBuilder builder = PostgresSqlTransportConnection.CreateBuilder(options);
        var settings = new PostgresSqlHostSettings(options);

        Assert.Equal("local:1234", builder.Host);
        Assert.Equal("local", settings.HostAddress.Host);
        Assert.Equal(1234, settings.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0031", "native-owner")]
    public void ConnectionBuilder_IncludesExplicitPostgresPort()
    {
        SqlTransportOptions options = Options("localhost");
        options.Port = 5544;

        NpgsqlConnectionStringBuilder builder = PostgresSqlTransportConnection.CreateBuilder(options);

        Assert.Equal(5544, builder.Port);
        Assert.Contains("Port=5544", builder.ConnectionString, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0032", "native-owner")]
    public void ConnectionBuilder_OmitsImplicitPostgresPort()
    {
        NpgsqlConnectionStringBuilder builder = PostgresSqlTransportConnection.CreateBuilder(Options("localhost"));

        Assert.Equal(NpgsqlConnection.DefaultPort, builder.Port);
        Assert.DoesNotContain("Port=", builder.ConnectionString, StringComparison.OrdinalIgnoreCase);
    }

    private static SqlTransportOptions Options(string host) => new()
    {
        Host = host,
        Database = "transport_tests",
        Schema = "transport",
        Role = "transport",
        Username = "test_user",
        Password = "test_password",
    };
}
