using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.SqlServer;

public sealed class SqlServerConnectionConfigurationTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0039", "native-owner")]
    public void ConnectionBuilder_IncludesInstanceName()
    {
        SqlTransportOptions options = Options(@"localhost\instance");

        SqlConnectionStringBuilder builder = SqlServerSqlTransportConnection.CreateBuilder(options);

        Assert.Equal(@"localhost\instance", builder.DataSource);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0042", "native-owner")]
    public void ConnectionBuilder_FormatsPortWithComma()
    {
        SqlTransportOptions options = Options("localhost");
        options.Port = 8675;

        SqlConnectionStringBuilder builder = SqlServerSqlTransportConnection.CreateBuilder(options);

        Assert.Equal("localhost,8675", builder.DataSource);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0043", "native-owner")]
    public void HostSettings_PreserveLocalDbDataSourceAndNormalizeBusHost()
    {
        var settings = new SqlServerSqlHostSettings(Options("(LocalDb)"));
        var builder = new SqlConnectionStringBuilder(settings.GetConnectionString());

        Assert.Equal("(LocalDb)", builder.DataSource);
        Assert.Equal("localdb", settings.HostAddress.Host);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0044", "native-owner")]
    public void ConnectionBuilder_OmitsUnspecifiedSqlServerPort()
    {
        SqlConnectionStringBuilder builder = SqlServerSqlTransportConnection.CreateBuilder(Options("localhost"));

        Assert.Equal("localhost", builder.DataSource);
        Assert.DoesNotContain("localhost,", builder.ConnectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0128", "native-owner")]
    public void ConnectionBuilder_MergesExplicitOptionsOverConnectionStringAndBackfillsMissingValues()
    {
        var optionsOnly = Options("option-host");
        optionsOnly.Database = "option-db";
        optionsOnly.Username = "option-user";
        optionsOnly.Password = "option-password";
        SqlConnectionStringBuilder fromOptions = SqlServerSqlTransportConnection.CreateBuilder(optionsOnly);

        var connectionOnly = new SqlTransportOptions
        {
            ConnectionString = "Data Source=connection-host,1544;Initial Catalog=connection-db;User ID=connection-user;Password=connection-password;TrustServerCertificate=False",
        };
        SqlConnectionStringBuilder fromConnection = SqlServerSqlTransportConnection.CreateBuilder(connectionOnly);

        var conflicting = Options("option-host");
        conflicting.Database = "option-db";
        conflicting.Username = "option-user";
        conflicting.Password = "option-password";
        conflicting.ConnectionString = "Data Source=connection-host,1544;Initial Catalog=connection-db;User ID=connection-user;Password=connection-password;TrustServerCertificate=False";
        SqlConnectionStringBuilder merged = SqlServerSqlTransportConnection.CreateBuilder(conflicting);

        Assert.Equal("option-host", fromOptions.DataSource);
        Assert.Equal("connection-host,1544", fromConnection.DataSource);
        Assert.Equal("connection-host", connectionOnly.Host);
        Assert.Equal(1544, connectionOnly.Port);
        Assert.Equal("connection-db", connectionOnly.Database);
        Assert.Equal("connection-user", connectionOnly.Username);
        Assert.Equal("connection-password", connectionOnly.Password);
        Assert.Equal("transport", connectionOnly.Schema);
        Assert.Equal("transport", connectionOnly.Role);
        Assert.Equal("option-host", merged.DataSource);
        Assert.Equal("option-db", merged.InitialCatalog);
        Assert.Equal("option-user", merged.UserID);
        Assert.Equal("option-password", merged.Password);
        Assert.True(fromOptions.TrustServerCertificate);
        Assert.True(fromConnection.TrustServerCertificate);
        Assert.True(merged.TrustServerCertificate);
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
