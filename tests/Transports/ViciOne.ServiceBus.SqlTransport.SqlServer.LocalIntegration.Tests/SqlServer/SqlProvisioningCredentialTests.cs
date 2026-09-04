using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlProvisioningCredentialTests
{
    private const string Password = "A-safe'password;--42!";

    [Theory]
    [InlineData("postgresql")]
    [InlineData("sqlserver")]
    [RequirementCoverage("OBL-R0-SQL-0129", "sql-native-security-owner")]
    public async Task ProvisionedAccountAcceptsQuotedPasswordWithoutPublishingItInStatementText(string provider)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        switch (provider)
        {
            case "postgresql":
                await VerifyPostgreSql(cancellationToken);
                break;
            case "sqlserver":
                await VerifySqlServer(cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
    }

    private static async Task VerifySqlServer(CancellationToken cancellationToken)
    {
        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.SqlServer);
        SqlServerLocalOptions provider = testOptions.LocalInfrastructure!.SqlServer!;
        string suffix = Guid.NewGuid().ToString("N")[..12];
        string database = $"vsb_credential_{suffix}";
        string username = $"vsb_login_{suffix}";
        var options = new SqlTransportOptions
        {
            Host = provider.Host,
            Port = provider.Port,
            Database = database,
            Schema = "transport",
            Role = "transport",
            Username = username,
            Password = Password,
            AdminUsername = provider.UserName,
            AdminPassword = provider.Password,
        };
        var migrator = new SqlServerDatabaseMigrator(NullLogger<SqlServerDatabaseMigrator>.Instance);
        bool databaseCreated = false;
        try
        {
            await migrator.CreateDatabase(options, cancellationToken);
            databaseCreated = true;
            await migrator.CreateSchemaIfNotExist(options, cancellationToken);

            var accountBuilder = new SqlConnectionStringBuilder
            {
                DataSource = $"{provider.Host},{provider.Port}",
                InitialCatalog = database,
                UserID = username,
                Password = Password,
                TrustServerCertificate = true,
            };
            await using (var account = new SqlConnection(accountBuilder.ConnectionString))
            {
                await account.OpenAsync(cancellationToken);
                await using var identity = new SqlCommand("SELECT ORIGINAL_LOGIN()", account);
                Assert.Equal(username, Assert.IsType<string>(await identity.ExecuteScalarAsync(cancellationToken)));
            }

            await using SqlConnection admin = SqlServerAdminConnection(provider, "master");
            await admin.OpenAsync(cancellationToken);
            Assert.Equal(0, await SqlServerCachedPasswordCount(admin, cancellationToken));
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(testOptions.OperationTimeout!.Value);
            if (databaseCreated)
                await migrator.DeleteDatabase(options, cleanup.Token);
            await using SqlConnection admin = SqlServerAdminConnection(provider, "master");
            await admin.OpenAsync(cleanup.Token);
            await using var drop = new SqlCommand(
                "IF EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = @Username) "
                + "BEGIN DECLARE @statement nvarchar(max) = N'DROP LOGIN ' + QUOTENAME(@Username); EXEC sys.sp_executesql @statement; END",
                admin);
            drop.Parameters.AddWithValue("Username", username);
            await drop.ExecuteNonQueryAsync(cleanup.Token);
        }
    }

    private static async Task VerifyPostgreSql(CancellationToken cancellationToken)
    {
        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.PostgreSql);
        PostgreSqlLocalOptions provider = testOptions.LocalInfrastructure!.PostgreSql!;
        string suffix = Guid.NewGuid().ToString("N")[..12];
        string database = $"vsb_credential_{suffix}";
        string username = $"vsb_user_{suffix}";
        string role = $"vsb_role_{suffix}";
        var options = new SqlTransportOptions
        {
            Host = provider.Host,
            Port = provider.Port,
            Database = database,
            Schema = "transport",
            Role = role,
            Username = username,
            Password = Password,
            AdminUsername = provider.UserName,
            AdminPassword = provider.Password,
        };
        var migrator = new PostgresDatabaseMigrator(NullLogger<PostgresDatabaseMigrator>.Instance);
        bool databaseCreated = false;
        try
        {
            await migrator.CreateDatabase(options, cancellationToken);
            databaseCreated = true;
            await migrator.CreateSchemaIfNotExist(options, cancellationToken);

            var accountBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = provider.Host,
                Port = provider.Port!.Value,
                Database = database,
                Username = username,
                Password = Password,
            };
            await using (var account = new NpgsqlConnection(accountBuilder.ConnectionString))
            {
                await account.OpenAsync(cancellationToken);
                await using var identity = new NpgsqlCommand("SELECT current_user", account);
                Assert.Equal(username, Assert.IsType<string>(await identity.ExecuteScalarAsync(cancellationToken)));
            }

            await using NpgsqlConnection admin = PostgreSqlAdminConnection(provider);
            await admin.OpenAsync(cancellationToken);
            await using var visible = new NpgsqlCommand(
                "SELECT COUNT(*) FROM pg_stat_activity WHERE query LIKE '%' || @password || '%'",
                admin);
            visible.Parameters.AddWithValue("password", Password);
            Assert.Equal(0L, Convert.ToInt64(await visible.ExecuteScalarAsync(cancellationToken)));
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(testOptions.OperationTimeout!.Value);
            if (databaseCreated)
                await migrator.DeleteDatabase(options, cleanup.Token);
            await using NpgsqlConnection admin = PostgreSqlAdminConnection(provider);
            await admin.OpenAsync(cleanup.Token);
            await using var drop = new NpgsqlCommand(
                $"DROP ROLE IF EXISTS \"{username}\"; DROP ROLE IF EXISTS \"{role}\";",
                admin);
            await drop.ExecuteNonQueryAsync(cleanup.Token);
        }
    }

    private static SqlConnection SqlServerAdminConnection(SqlServerLocalOptions provider, string database)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{provider.Host},{provider.Port}",
            InitialCatalog = database,
            UserID = provider.UserName,
            Password = provider.Password,
            TrustServerCertificate = true,
        };
        return new SqlConnection(builder.ConnectionString);
    }

    private static async Task<int> SqlServerCachedPasswordCount(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM sys.dm_exec_cached_plans plans "
            + "CROSS APPLY sys.dm_exec_sql_text(plans.plan_handle) text "
            + "WHERE text.text LIKE '%' + @password + '%'",
            connection);
        command.Parameters.AddWithValue("password", Password);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static NpgsqlConnection PostgreSqlAdminConnection(PostgreSqlLocalOptions provider)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = provider.Host,
            Port = provider.Port!.Value,
            Database = provider.Database,
            Username = provider.UserName,
            Password = provider.Password,
        };
        return new NpgsqlConnection(builder.ConnectionString);
    }
}
