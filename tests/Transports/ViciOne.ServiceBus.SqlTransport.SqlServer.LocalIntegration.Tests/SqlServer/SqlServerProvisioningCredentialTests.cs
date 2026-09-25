using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerProvisioningCredentialTests
{
    private const string Password = "A-safe'password;--42!";

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0129", "sqlserver-native-security-owner")]
    public async Task ProvisionedAccountAcceptsQuotedPasswordWithoutPublishingItInStatementTextAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.SqlServer);
        SqlServerLocalOptions provider = testOptions.LocalInfrastructure!.SqlServer!;
        string suffix = Guid.NewGuid().ToString("N")[..12];
        string database = $"vsb_credential_{suffix}";
        string username = $"vsb_login_{suffix}";
        var connectionBuilder = new SqlConnectionStringBuilder
        {
            DataSource = $"{provider.Host},{provider.Port}",
            InitialCatalog = provider.Database,
            UserID = provider.UserName,
            Password = provider.Password,
            TrustServerCertificate = true,
        };
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
            ConnectionString = connectionBuilder.ConnectionString,
        };
        var migrator = new SqlServerDatabaseMigrator(NullLogger<SqlServerDatabaseMigrator>.Instance);
        bool databaseCreated = false;
        try
        {
            await migrator.CreateDatabaseAsync(options, cancellationToken);
            databaseCreated = true;
            await migrator.CreateSchemaIfNotExistAsync(options, cancellationToken);

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

            await using (var adminDatabase = SqlServerAdminConnection(provider, database))
            {
                await adminDatabase.OpenAsync(cancellationToken);
                await using var revoke = new SqlCommand("REVOKE CREATE VIEW FROM [transport]", adminDatabase);
                await revoke.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var deniedAccount = new SqlConnection(accountBuilder.ConnectionString))
            {
                await deniedAccount.OpenAsync(cancellationToken);
                await using var deniedView = new SqlCommand(
                    "CREATE VIEW [transport].[ProvisioningPermissionProbe] AS SELECT 42 AS [Value]",
                    deniedAccount);
                SqlException denial = await Assert.ThrowsAsync<SqlException>(
                    () => deniedView.ExecuteNonQueryAsync(cancellationToken));
                Assert.Equal(262, denial.Number);
            }

            await migrator.CreateSchemaIfNotExistAsync(options, cancellationToken);

            await using (var account = new SqlConnection(accountBuilder.ConnectionString))
            {
                await account.OpenAsync(cancellationToken);
                await using var create = new SqlCommand("CREATE VIEW [transport].[ProvisioningPermissionProbe] AS SELECT 42 AS [Value]", account);
                await create.ExecuteNonQueryAsync(cancellationToken);
                await using var query = new SqlCommand("SELECT [Value] FROM [transport].[ProvisioningPermissionProbe]", account);
                Assert.Equal(42, Convert.ToInt32(await query.ExecuteScalarAsync(cancellationToken)));
            }

            await using SqlConnection admin = SqlServerAdminConnection(provider, "master");
            await admin.OpenAsync(cancellationToken);
            Assert.Equal(0, await SqlServerCachedPasswordCountAsync(admin, cancellationToken));
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(testOptions.OperationTimeout!.Value);
            if (databaseCreated)
                await migrator.DeleteDatabaseAsync(options, cleanup.Token);
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

    private static async Task<int> SqlServerCachedPasswordCountAsync(
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
}
