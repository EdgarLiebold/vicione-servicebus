using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlProvisioningCredentialTests
{
    private const string Password = "A-safe'password;--42!";

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0129", "postgresql-native-security-owner")]
    public async Task ProvisionedAccountAcceptsQuotedPasswordWithoutPublishingItInStatementTextAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        var migrator = new PostgreSqlDatabaseMigrator(NullLogger<PostgreSqlDatabaseMigrator>.Instance);
        bool databaseCreated = false;
        try
        {
            await migrator.CreateDatabaseAsync(options, cancellationToken);
            databaseCreated = true;
            await migrator.CreateSchemaIfNotExistAsync(options, cancellationToken);

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
                await migrator.DeleteDatabaseAsync(options, cleanup.Token);
            await using NpgsqlConnection admin = PostgreSqlAdminConnection(provider);
            await admin.OpenAsync(cleanup.Token);
            await using var drop = new NpgsqlCommand(
                $"DROP ROLE IF EXISTS \"{username}\"; DROP ROLE IF EXISTS \"{role}\";",
                admin);
            await drop.ExecuteNonQueryAsync(cleanup.Token);
        }
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
