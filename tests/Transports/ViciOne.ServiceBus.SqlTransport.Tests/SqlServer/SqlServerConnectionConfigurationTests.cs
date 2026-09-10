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

        SqlConnectionStringBuilder builder = SqlServerTransportConnection.CreateBuilder(options);

        Assert.Equal(@"localhost\instance", builder.DataSource);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0042", "native-owner")]
    public void ConnectionBuilder_FormatsPortWithComma()
    {
        SqlTransportOptions options = Options("localhost");
        options.Port = 8675;

        SqlConnectionStringBuilder builder = SqlServerTransportConnection.CreateBuilder(options);

        Assert.Equal("localhost,8675", builder.DataSource);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0043", "native-owner")]
    public void HostSettings_PreserveLocalDbDataSourceAndNormalizeBusHost()
    {
        var settings = new SqlServerHostSettings(Options("(LocalDb)"));
        var builder = new SqlConnectionStringBuilder(settings.GetConnectionString());

        Assert.Equal("(LocalDb)", builder.DataSource);
        Assert.Equal("localdb", settings.HostAddress.Host);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0044", "native-owner")]
    public void ConnectionBuilder_OmitsUnspecifiedSqlServerPort()
    {
        SqlConnectionStringBuilder builder = SqlServerTransportConnection.CreateBuilder(Options("localhost"));

        Assert.Equal("localhost", builder.DataSource);
        Assert.DoesNotContain("localhost,", builder.ConnectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0128", "native-owner")]
    public void ConnectionBuilder_MergesExplicitOptionsWithoutMutatingOptionsOrWeakeningCertificateValidation()
    {
        var optionsOnly = Options("option-host");
        optionsOnly.Database = "option-db";
        optionsOnly.Username = "option-user";
        optionsOnly.Password = "option-password";
        SqlConnectionStringBuilder fromOptions = SqlServerTransportConnection.CreateBuilder(optionsOnly);

        var connectionOnly = new SqlTransportOptions
        {
            ConnectionString = "Data Source=connection-host,1544;Initial Catalog=connection-db;User ID=connection-user;Password=connection-password;TrustServerCertificate=False",
        };
        SqlConnectionStringBuilder fromConnection = SqlServerTransportConnection.CreateBuilder(connectionOnly);

        var conflicting = Options("option-host");
        conflicting.Database = "option-db";
        conflicting.Username = "option-user";
        conflicting.Password = "option-password";
        conflicting.ConnectionString = "Data Source=connection-host,1544;Initial Catalog=connection-db;User ID=connection-user;Password=connection-password;TrustServerCertificate=False";
        SqlConnectionStringBuilder merged = SqlServerTransportConnection.CreateBuilder(conflicting);

        Assert.Equal("option-host", fromOptions.DataSource);
        Assert.Equal("connection-host,1544", fromConnection.DataSource);
        Assert.Null(connectionOnly.Host);
        Assert.Null(connectionOnly.Port);
        Assert.Null(connectionOnly.Database);
        Assert.Null(connectionOnly.Username);
        Assert.Null(connectionOnly.Password);
        Assert.Equal("transport", connectionOnly.Schema);
        Assert.Equal("transport", connectionOnly.Role);
        Assert.Equal("option-host", merged.DataSource);
        Assert.Equal("option-db", merged.InitialCatalog);
        Assert.Equal("option-user", merged.UserID);
        Assert.Equal("option-password", merged.Password);
        Assert.False(fromOptions.TrustServerCertificate);
        Assert.False(fromConnection.TrustServerCertificate);
        Assert.False(merged.TrustServerCertificate);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQLSERVER-HOST-PROJECTION", "current-settings-are-used-without-security-downgrade")]
    public void HostSettings_RebuildConnectionStringFromCurrentValuesAndPreserveSecurityOptions()
    {
        var settings = new SqlServerHostSettings(
            "Data Source=initial;Initial Catalog=before;User ID=old;Password=old-password;TrustServerCertificate=False");

        settings.Host = "current";
        settings.InstanceName = "named";
        settings.Port = 1444;
        settings.Database = "after";
        settings.Username = "new";
        settings.Password = "new-password";

        var builder = new SqlConnectionStringBuilder(settings.GetConnectionString());

        Assert.Equal(@"current\named,1444", builder.DataSource);
        Assert.Equal("after", builder.InitialCatalog);
        Assert.Equal("new", builder.UserID);
        Assert.Equal("new-password", builder.Password);
        Assert.False(builder.TrustServerCertificate);
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
