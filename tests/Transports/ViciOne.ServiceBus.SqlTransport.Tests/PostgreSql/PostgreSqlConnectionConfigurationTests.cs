using System.Text;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.PostgreSql;

public sealed class PostgreSqlConnectionConfigurationTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0022", "native-owner")]
    public void NotifyChannel_UsesStableNamespaceHashAndQueueId()
    {
        string actual = PostgreSqlNotificationChannel.CreateName("transport", 42);

        Assert.Equal("vsb_6694ea8075001f6628da20f1afdafc74a7_msg_42", actual);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0023", "native-owner")]
    public void NotifyChannel_DistinguishesSchemasThatShareALongPrefix()
    {
        string commonPrefix = new('x', 1_000);

        string first = PostgreSqlNotificationChannel.CreateName($"{commonPrefix}-first", 42);
        string second = PostgreSqlNotificationChannel.CreateName($"{commonPrefix}-second", 42);

        Assert.NotEqual(first, second);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0024", "native-owner")]
    public void NotifyChannel_DefaultsMissingSchema()
    {
        string?[] schemas = [null, string.Empty, " \t"];
        string expected = PostgreSqlNotificationChannel.CreateName("transport", 42);

        Assert.All(schemas, schema => Assert.Equal(expected, PostgreSqlNotificationChannel.CreateName(schema, 42)));
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    [RequirementCoverage("REQ-VSB-POSTGRES-NOTIFY-CHANNEL", "utf8-identifier-budget")]
    public void NotifyChannel_UsesAtMostSixtyThreeBytesForUnicodeSchemaAndAnyQueueId(long queueId)
    {
        const string schema = "租户-🚚-äöü-very-long-schema-name-that-exceeds-the-original-character-budget";

        string actual = PostgreSqlNotificationChannel.CreateName(schema, queueId);

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
            await using PostgreSqlTransportConnection connection =
                PostgreSqlTransportConnection.GetDatabaseAdminConnection(options);
            var projected = new NpgsqlConnectionStringBuilder(connection.Connection.ConnectionString);

            Assert.Equal("sample", projected.Database);
            Assert.Equal("test-password", projected.Password);
            Assert.Equal(source.Username, projected.Username);
            Assert.Equal(source.Host, projected.Host);
            Assert.Equal("admin", PostgreSqlTransportConnection.GetAdminMigrationPrincipal(options));
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0027", "native-owner")]
    public async Task HostSettings_PreserveMultipleHostsAndUseFirstBusHostAsync()
    {
        var settings = new PostgreSqlHostSettings(Options("local,remote"));
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
        var settings = new PostgreSqlHostSettings(options);
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
        var settings = new PostgreSqlHostSettings(Options("local:1234,remote:5678"));
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
        NpgsqlConnectionStringBuilder builder = PostgreSqlTransportConnection.CreateBuilder(options);
        var settings = new PostgreSqlHostSettings(options);

        Assert.Equal("local:1234", builder.Host);
        Assert.Equal("local", settings.HostAddress.Host);
        Assert.Equal(1234, settings.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "ipv6-address-is-not-split-as-host-and-port")]
    public void HostSettings_ProjectIpv6HostWithoutTreatingAddressSegmentsAsAPort()
    {
        var settings = new PostgreSqlHostSettings(Options("::1"));

        Assert.Equal("[::1]", settings.HostAddress.Host);
        Assert.Equal(-1, settings.HostAddress.Port);
    }

    [Theory]
    [InlineData("local", "local", -1, null)]
    [InlineData("local:1234", "local", 1234, null)]
    [InlineData("local:5432", "local", -1, null)]
    [InlineData("::1", "[::1]", -1, null)]
    [InlineData("[::1]", "[::1]", -1, null)]
    [InlineData("[::1]:5544", "[::1]", 5544, null)]
    [InlineData("local:1234,remote:5678", "local", -1, "local:1234,remote:5678")]
    [InlineData("[::1]:1234,[::2]:5678", "[::1]", -1, "[::1]:1234,[::2]:5678")]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "supported-single-ipv6-and-multiple-host-shapes")]
    public void HostSettings_ProjectEverySupportedHostShape(
        string configuredHost,
        string addressHost,
        int addressPort,
        string? multipleHosts)
    {
        var settings = new PostgreSqlHostSettings(Options(configuredHost));

        Assert.Equal(addressHost, settings.HostAddress.Host);
        Assert.Equal(addressPort, settings.HostAddress.Port);
        Assert.Equal(multipleHosts, settings.MultipleHosts);
    }

    [Theory]
    [InlineData("local:0")]
    [InlineData("local:65536")]
    [InlineData("local:not-a-port")]
    [InlineData("[::1]:0")]
    [InlineData("[::1]:65536")]
    [InlineData("[::1]:not-a-port")]
    [InlineData("[::1")]
    [InlineData("[::1]garbage")]
    [InlineData("local,,remote")]
    [InlineData("local,remote:not-a-port")]
    [InlineData("local,[::1]garbage")]
    [InlineData("2001:db8::invalid")]
    [InlineData("/var/run/postgresql")]
    [InlineData("@transport")]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "malformed-host-segments-fail-at-configuration-boundary")]
    public void HostSettings_RejectMalformedHostSegmentsBeforeRuntime(string configuredHost)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new PostgreSqlHostSettings(Options(configuredHost)));

        Assert.Equal("host", exception.ParamName);
    }

    [Theory]
    [InlineData(1234, 1234)]
    [InlineData(5432, -1)]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "connection-string-inline-port-matches-data-source")]
    public async Task ConnectionStringInlinePort_UsesTheSameBusAndDataSourceTargetAsync(
        int inlinePort,
        int expectedAddressPort)
    {
        var settings = new PostgreSqlHostSettings(
            $"Host=local:{inlinePort};Port=5544;Database=transport_tests;Username=test_user;Password=test_password");

        await using NpgsqlDataSource dataSource = settings.GetDataSource();
        var builder = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Equal("local", settings.HostAddress.Host);
        Assert.Equal(expectedAddressPort, settings.HostAddress.Port);
        Assert.Equal("local", builder.Host);
        Assert.Equal(inlinePort, builder.Port);
    }

    [Theory]
    [InlineData(1234, 1234)]
    [InlineData(5432, -1)]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "options-inline-port-matches-data-source")]
    public async Task OptionsInlinePort_UsesTheSameBusAndDataSourceTargetAsync(
        int inlinePort,
        int expectedAddressPort)
    {
        SqlTransportOptions options = Options($"local:{inlinePort}");
        options.Port = 5544;
        var settings = new PostgreSqlHostSettings(options);

        await using NpgsqlDataSource dataSource = settings.GetDataSource();
        var builder = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Equal("local", settings.HostAddress.Host);
        Assert.Equal(expectedAddressPort, settings.HostAddress.Port);
        Assert.Equal("local", builder.Host);
        Assert.Equal(inlinePort, builder.Port);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "missing-host-remains-a-validation-failure")]
    public void HostSettings_ReportMissingHostThroughConfigurationValidation(string? configuredHost)
    {
        var settings = new PostgreSqlHostSettings(Options(configuredHost));

        ValidationResult failure = Assert.Single(settings.Validate());

        Assert.Equal("Host", failure.Key);
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Throws<ConfigurationException>(() => settings.HostAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "connection-string-replacement-clears-derived-state")]
    public void ConnectionStringReplacement_ClearsPriorMultipleHostAndPortProjection()
    {
        var settings = new PostgreSqlHostSettings(
            "Host=first,second;Port=5544;Database=transport_tests;Username=test_user;Password=test_password");

        settings.ConnectionString = "Host=final;Database=transport_tests;Username=test_user;Password=test_password";

        Assert.Null(settings.MultipleHosts);
        Assert.Equal("final", settings.HostAddress.Host);
        Assert.Equal(-1, settings.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "failed-connection-string-replacement-is-atomic")]
    public async Task FailedConnectionStringReplacement_PreservesEveryPriorSettingAsync()
    {
        const string initial =
            "Host=first,second;Port=5544;Database=before;Username=old;Password=old-password;Persist Security Info=true;Application Name=baseline-app";
        var settings = new PostgreSqlHostSettings(initial);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            settings.ConnectionString =
                "Host=valid,broken:not-a-port;Database=after;Username=new;Password=new-password");

        await using NpgsqlDataSource dataSource = settings.GetDataSource();
        var builder = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Equal("host", exception.ParamName);
        Assert.Equal("first,second", settings.MultipleHosts);
        Assert.Equal("first", settings.HostAddress.Host);
        Assert.Equal(5544, settings.HostAddress.Port);
        Assert.Equal("first,second", builder.Host);
        Assert.Equal(5544, builder.Port);
        Assert.Equal("before", builder.Database);
        Assert.Equal("old", builder.Username);
        Assert.Equal("old-password", builder.Password);
        Assert.Equal("baseline-app", builder.ApplicationName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "current-settings-rebuild-data-source-without-security-downgrade")]
    public async Task HostSettings_RebuildDataSourceFromCurrentValuesAndPreserveSecurityOptionsAsync()
    {
        var settings = new PostgreSqlHostSettings(
            "Host=first,second;Port=5544;Database=before;Username=old;Password=old-password;Persist Security Info=true;Search Path=before_schema;SSL Mode=Require;Application Name=baseline-app");

        settings.Host = "current";
        settings.Port = null;
        settings.Database = "after";
        settings.Username = "new";
        settings.Password = "new-password";
        settings.Schema = "after_schema";

        await using NpgsqlDataSource dataSource = settings.GetDataSource();
        var builder = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Equal("current", builder.Host);
        Assert.Equal(NpgsqlConnection.DefaultPort, builder.Port);
        Assert.Equal("after", builder.Database);
        Assert.Equal("new", builder.Username);
        Assert.Equal("new-password", builder.Password);
        Assert.Equal("after_schema", builder.SearchPath);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal("baseline-app", builder.ApplicationName);
        Assert.Equal("current", settings.HostAddress.Host);
        Assert.Equal(-1, settings.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-HOST-PROJECTION", "explicit-first-host-override-collapses-multiple-host-list")]
    public async Task HostSettings_ExplicitFirstHostOverride_CollapsesPriorMultipleHostListAsync()
    {
        var settings = new PostgreSqlHostSettings(
            "Host=first,second;Database=transport_tests;Username=test_user;Password=test_password");

        settings.Host = "first";

        await using NpgsqlDataSource dataSource = settings.GetDataSource();
        var builder = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Null(settings.MultipleHosts);
        Assert.Equal("first", settings.HostAddress.Host);
        Assert.Equal("first", builder.Host);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0031", "native-owner")]
    public void ConnectionBuilder_IncludesExplicitPostgreSqlPort()
    {
        SqlTransportOptions options = Options("localhost");
        options.Port = 5544;

        NpgsqlConnectionStringBuilder builder = PostgreSqlTransportConnection.CreateBuilder(options);

        Assert.Equal(5544, builder.Port);
        Assert.Contains("Port=5544", builder.ConnectionString, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0032", "native-owner")]
    public void ConnectionBuilder_OmitsImplicitPostgreSqlPort()
    {
        NpgsqlConnectionStringBuilder builder = PostgreSqlTransportConnection.CreateBuilder(Options("localhost"));

        Assert.Equal(NpgsqlConnection.DefaultPort, builder.Port);
        Assert.DoesNotContain("Port=", builder.ConnectionString, StringComparison.OrdinalIgnoreCase);
    }

    private static SqlTransportOptions Options(string? host) => new()
    {
        Host = host,
        Database = "transport_tests",
        Schema = "transport",
        Role = "transport",
        Username = "test_user",
        Password = "test_password",
    };
}
