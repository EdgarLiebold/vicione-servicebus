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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("OBL-R0-SQL-0129", "principal-kind-collision-does-not-grant-access")]
    public async Task PrincipalKindMismatch_IsRejectedWithoutTransferringOwnershipOrPermissionsAsync(bool usernameIsRole)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.SqlServer);
        SqlServerLocalOptions provider = testOptions.LocalInfrastructure!.SqlServer!;
        string database = $"vsb_role_collision_{Guid.NewGuid():N}";
        string username = $"vsb_collision_{Guid.NewGuid():N}";
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
            ConnectionString = "TrustServerCertificate=true",
        };
        var migrator = new SqlServerDatabaseMigrator(NullLogger<SqlServerDatabaseMigrator>.Instance);
        bool created = false;
        try
        {
            await migrator.CreateDatabaseAsync(options, cancellationToken);
            created = true;
            await using var admin = SqlServerAdminConnection(provider, database);
            await admin.OpenAsync(cancellationToken);
            await using (var setup = new SqlCommand(
                (usernameIsRole
                    ? $"CREATE ROLE [{username}]; CREATE USER [probe] WITHOUT LOGIN; ALTER ROLE [{username}] ADD MEMBER [probe]; "
                    : "CREATE USER [transport] WITHOUT LOGIN; ")
                + "EXEC('CREATE SCHEMA [transport] AUTHORIZATION [dbo]');", admin))
                await setup.ExecuteNonQueryAsync(cancellationToken);

            string probeUser = usernameIsRole ? "probe" : "transport";
            Assert.Equal("dbo", await SchemaOwnerAsync(admin, cancellationToken));
            Assert.Equal(0, await CollidingUserCreateViewPermissionAsync(admin, probeUser, cancellationToken));

            Exception? failure = await Record.ExceptionAsync(() => migrator.CreateSchemaIfNotExistAsync(options, cancellationToken));

            string ownerAfter = await SchemaOwnerAsync(admin, cancellationToken);
            int permissionAfter = await CollidingUserCreateViewPermissionAsync(admin, probeUser, cancellationToken);
            Assert.True(ownerAfter == "dbo" && permissionAfter == 0,
                $"Rejected principal collision changed schema owner to '{ownerAfter}' and CREATE VIEW permission to {permissionAfter}.");
            InvalidOperationException rejection = Assert.IsType<InvalidOperationException>(failure);
            Assert.Contains(usernameIsRole ? username : "transport", rejection.Message, StringComparison.Ordinal);
            Assert.Contains("role", rejection.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(testOptions.OperationTimeout!.Value);
            if (created)
                await migrator.DeleteDatabaseAsync(options, cleanup.Token);
            await using var admin = SqlServerAdminConnection(provider, "master");
            await admin.OpenAsync(cleanup.Token);
            await using var drop = new SqlCommand($"IF SUSER_ID('{username}') IS NOT NULL DROP LOGIN [{username}]", admin);
            await drop.ExecuteNonQueryAsync(cleanup.Token);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0129", "connection-string-identity-and-membership-repair")]
    public async Task ConnectionStringAccount_RegainsEffectivePermissionsAfterRoleMembershipIsRemovedAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.SqlServer);
        SqlServerLocalOptions provider = testOptions.LocalInfrastructure!.SqlServer!;
        string suffix = Guid.NewGuid().ToString("N");
        string database = $"vsb_membership_{suffix}";
        string username = $"vsb_member_{suffix}";
        var accountBuilder = new SqlConnectionStringBuilder
        {
            DataSource = $"{provider.Host},{provider.Port}",
            InitialCatalog = database,
            UserID = username,
            Password = Password,
            TrustServerCertificate = true,
        };
        var options = new SqlTransportOptions
        {
            ConnectionString = accountBuilder.ConnectionString,
            Schema = "transport",
            Role = "transport",
            AdminUsername = provider.UserName,
            AdminPassword = provider.Password,
        };
        var migrator = new SqlServerDatabaseMigrator(NullLogger<SqlServerDatabaseMigrator>.Instance);
        bool created = false;
        try
        {
            await migrator.CreateDatabaseAsync(options, cancellationToken);
            created = true;
            await migrator.CreateSchemaIfNotExistAsync(options, cancellationToken);
            await using (var account = new SqlConnection(accountBuilder.ConnectionString))
            {
                await account.OpenAsync(cancellationToken);
                await using var identity = new SqlCommand("SELECT ORIGINAL_LOGIN()", account);
                Assert.Equal(username, await identity.ExecuteScalarAsync(cancellationToken));
                await using var create = new SqlCommand("CREATE VIEW [transport].[InitialProbe] AS SELECT 42 AS [Value]", account);
                await create.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var admin = SqlServerAdminConnection(provider, database))
            {
                await admin.OpenAsync(cancellationToken);
                Assert.Equal(1, await MembershipCountAsync(admin, username, cancellationToken));
                Assert.Equal(0, await MembershipCountAsync(admin, "dbo", cancellationToken));
                await using var remove = new SqlCommand($"ALTER ROLE [transport] DROP MEMBER [{username}]", admin);
                await remove.ExecuteNonQueryAsync(cancellationToken);
                Assert.Equal(0, await MembershipCountAsync(admin, username, cancellationToken));
            }

            await using (var denied = new SqlConnection(accountBuilder.ConnectionString))
            {
                await denied.OpenAsync(cancellationToken);
                await using var create = new SqlCommand("CREATE VIEW [transport].[RestoredProbe] AS SELECT 43 AS [Value]", denied);
                SqlException failure = await Assert.ThrowsAsync<SqlException>(() => create.ExecuteNonQueryAsync(cancellationToken));
                Assert.Equal(262, failure.Number);
            }

            await migrator.CreateSchemaIfNotExistAsync(options, cancellationToken);
            await using (var account = new SqlConnection(accountBuilder.ConnectionString))
            {
                await account.OpenAsync(cancellationToken);
                await using var create = new SqlCommand("CREATE VIEW [transport].[RestoredProbe] AS SELECT 43 AS [Value]", account);
                await create.ExecuteNonQueryAsync(cancellationToken);
                await using var query = new SqlCommand("SELECT [Value] FROM [transport].[RestoredProbe]", account);
                Assert.Equal(43, Convert.ToInt32(await query.ExecuteScalarAsync(cancellationToken)));
            }
            await using var verification = SqlServerAdminConnection(provider, database);
            await verification.OpenAsync(cancellationToken);
            Assert.Equal(1, await MembershipCountAsync(verification, username, cancellationToken));
            Assert.Equal(0, await MembershipCountAsync(verification, "dbo", cancellationToken));
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(testOptions.OperationTimeout!.Value);
            if (created)
                await migrator.DeleteDatabaseAsync(options, cleanup.Token);
            await using var admin = SqlServerAdminConnection(provider, "master");
            await admin.OpenAsync(cleanup.Token);
            await using var drop = new SqlCommand($"IF SUSER_ID('{username}') IS NOT NULL DROP LOGIN [{username}]", admin);
            await drop.ExecuteNonQueryAsync(cleanup.Token);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("OBL-R0-SQL-0129", "same-name-user-for-another-login-is-rejected")]
    public async Task SameNameUserMappedToAnotherLogin_DoesNotGrantThatLoginTransportPermissionsAsync(bool intendedLoginIsAdministrator)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.SqlServer);
        SqlServerLocalOptions provider = testOptions.LocalInfrastructure!.SqlServer!;
        string suffix = Guid.NewGuid().ToString("N");
        string database = $"vsb_sid_{suffix}";
        string username = $"vsb_intended_{suffix}";
        string otherLogin = $"vsb_other_{suffix}";
        var accountBuilder = new SqlConnectionStringBuilder
        {
            DataSource = $"{provider.Host},{provider.Port}",
            InitialCatalog = database,
            UserID = username,
            Password = Password,
            TrustServerCertificate = true,
        };
        var options = new SqlTransportOptions
        {
            ConnectionString = accountBuilder.ConnectionString,
            Schema = "transport",
            Role = "transport",
            AdminUsername = provider.UserName,
            AdminPassword = provider.Password,
        };
        var migrator = new SqlServerDatabaseMigrator(NullLogger<SqlServerDatabaseMigrator>.Instance);
        bool created = false;
        try
        {
            await migrator.CreateDatabaseAsync(options, cancellationToken);
            created = true;
            await using (var admin = SqlServerAdminConnection(provider, "master"))
            {
                await admin.OpenAsync(cancellationToken);
                await using var createLogin = new SqlCommand(
                    "DECLARE @statement nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@Username) "
                    + "+ N' WITH PASSWORD = ' + QUOTENAME(@Password, '''') + N';'; EXEC sys.sp_executesql @statement;", admin);
                createLogin.Parameters.AddWithValue("Username", otherLogin);
                createLogin.Parameters.AddWithValue("Password", Password);
                await createLogin.ExecuteNonQueryAsync(cancellationToken);
                if (intendedLoginIsAdministrator)
                {
                    await using var elevate = new SqlCommand($"ALTER SERVER ROLE [sysadmin] ADD MEMBER [{username}]", admin);
                    await elevate.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            await using var databaseAdmin = SqlServerAdminConnection(provider, database);
            await databaseAdmin.OpenAsync(cancellationToken);
            await using (var setup = new SqlCommand(
                $"CREATE USER [{username}] FOR LOGIN [{otherLogin}]; EXEC('CREATE SCHEMA [transport] AUTHORIZATION [dbo]');", databaseAdmin))
                await setup.ExecuteNonQueryAsync(cancellationToken);

            accountBuilder.UserID = otherLogin;
            await using var otherAccount = new SqlConnection(accountBuilder.ConnectionString);
            await otherAccount.OpenAsync(cancellationToken);
            await using (var identity = new SqlCommand("SELECT ORIGINAL_LOGIN()", otherAccount))
                Assert.Equal(otherLogin, await identity.ExecuteScalarAsync(cancellationToken));
            await using (var identity = new SqlCommand("SELECT CURRENT_USER", otherAccount))
                Assert.Equal(username, await identity.ExecuteScalarAsync(cancellationToken));
            Assert.Equal(0, await CreateViewPermissionAsync(otherAccount, cancellationToken));

            Exception? failure = await Record.ExceptionAsync(() => migrator.CreateSchemaIfNotExistAsync(options, cancellationToken));

            string ownerAfter = await SchemaOwnerAsync(databaseAdmin, cancellationToken);
            int permissionAfter = await CreateViewPermissionAsync(otherAccount, cancellationToken);
            Assert.True(ownerAfter == "dbo" && permissionAfter == 0,
                $"Mismapped user changed schema owner to '{ownerAfter}' and other login's CREATE VIEW permission to {permissionAfter}.");
            InvalidOperationException rejection = Assert.IsType<InvalidOperationException>(failure);
            Assert.Contains(username, rejection.Message, StringComparison.Ordinal);
            Assert.Contains("login", rejection.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(testOptions.OperationTimeout!.Value);
            if (created)
                await migrator.DeleteDatabaseAsync(options, cleanup.Token);
            await using var admin = SqlServerAdminConnection(provider, "master");
            await admin.OpenAsync(cleanup.Token);
            foreach (string login in new[] { username, otherLogin })
            {
                await using var drop = new SqlCommand($"IF SUSER_ID('{login}') IS NOT NULL DROP LOGIN [{login}]", admin);
                await drop.ExecuteNonQueryAsync(cleanup.Token);
            }
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0129", "contained-user-provisioning-preserves-database-identity")]
    public async Task ContainedSqlUser_CanBeProvisionedWithoutAServerLoginAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.SqlServer);
        SqlServerLocalOptions provider = testOptions.LocalInfrastructure!.SqlServer!;
        string suffix = Guid.NewGuid().ToString("N");
        string database = $"vsb_contained_{suffix}";
        string username = $"vsb_contained_user_{suffix}";
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{provider.Host},{provider.Port}",
            InitialCatalog = database,
            UserID = provider.UserName,
            Password = provider.Password,
            TrustServerCertificate = true,
        };
        var options = new SqlTransportOptions
        {
            ConnectionString = builder.ConnectionString,
            Schema = "transport",
            Role = "transport",
            AdminUsername = provider.UserName,
            AdminPassword = provider.Password,
        };
        var migrator = new SqlServerDatabaseMigrator(NullLogger<SqlServerDatabaseMigrator>.Instance);
        await using var master = SqlServerAdminConnection(provider, "master");
        await master.OpenAsync(cancellationToken);
        int originalSetting;
        await using (var query = new SqlCommand("SELECT value_in_use FROM sys.configurations WHERE name = 'contained database authentication'", master))
            originalSetting = Convert.ToInt32(await query.ExecuteScalarAsync(cancellationToken));
        bool created = false;
        try
        {
            await using (var enable = new SqlCommand("EXEC sp_configure 'contained database authentication', 1; RECONFIGURE;", master))
                await enable.ExecuteNonQueryAsync(cancellationToken);
            await migrator.CreateDatabaseAsync(options, cancellationToken);
            created = true;
            await using (var contain = new SqlCommand($"ALTER DATABASE [{database}] SET CONTAINMENT = PARTIAL", master))
                await contain.ExecuteNonQueryAsync(cancellationToken);
            await using (var admin = SqlServerAdminConnection(provider, database))
            {
                await admin.OpenAsync(cancellationToken);
                await using var create = new SqlCommand(
                    "DECLARE @statement nvarchar(max) = N'CREATE USER ' + QUOTENAME(@Username) "
                    + "+ N' WITH PASSWORD = ' + QUOTENAME(@Password, '''') + N';'; EXEC sys.sp_executesql @statement;", admin);
                create.Parameters.AddWithValue("Username", username);
                create.Parameters.AddWithValue("Password", Password);
                await create.ExecuteNonQueryAsync(cancellationToken);
            }
            builder.UserID = username;
            builder.Password = Password;
            options.ConnectionString = builder.ConnectionString;

            await migrator.CreateSchemaIfNotExistAsync(options, cancellationToken);
            await migrator.CreateSchemaIfNotExistAsync(options, cancellationToken);

            await using (var account = new SqlConnection(builder.ConnectionString))
            {
                await account.OpenAsync(cancellationToken);
                await using var identity = new SqlCommand("SELECT CURRENT_USER", account);
                Assert.Equal(username, await identity.ExecuteScalarAsync(cancellationToken));
                await using var create = new SqlCommand("CREATE VIEW [transport].[ContainedProbe] AS SELECT 47 AS [Value]", account);
                await create.ExecuteNonQueryAsync(cancellationToken);
                await using var query = new SqlCommand("SELECT [Value] FROM [transport].[ContainedProbe]", account);
                Assert.Equal(47, Convert.ToInt32(await query.ExecuteScalarAsync(cancellationToken)));
            }
            await using var serverLogin = new SqlCommand("SELECT COUNT(*) FROM sys.sql_logins WHERE name = @Username", master);
            serverLogin.Parameters.AddWithValue("Username", username);
            Assert.Equal(0, Convert.ToInt32(await serverLogin.ExecuteScalarAsync(cancellationToken)));
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(testOptions.OperationTimeout!.Value);
            try
            {
                if (created)
                    await migrator.DeleteDatabaseAsync(options, cleanup.Token);
            }
            finally
            {
                await using var restore = new SqlCommand("EXEC sp_configure 'contained database authentication', @Value; RECONFIGURE;", master);
                restore.Parameters.AddWithValue("Value", originalSetting);
                await restore.ExecuteNonQueryAsync(cleanup.Token);
            }
        }
    }

    private static async Task<int> CreateViewPermissionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE VIEW')", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<string> SchemaOwnerAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT USER_NAME(principal_id) FROM sys.schemas WHERE name = 'transport'", connection);
        return Assert.IsType<string>(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> CollidingUserCreateViewPermissionAsync(SqlConnection connection, string username, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"EXECUTE AS USER = '{username}'; SELECT HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE VIEW'); REVERT;", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> MembershipCountAsync(SqlConnection connection, string username, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM sys.database_role_members WHERE role_principal_id = DATABASE_PRINCIPAL_ID('transport') "
            + "AND member_principal_id = DATABASE_PRINCIPAL_ID(@Username)", connection);
        command.Parameters.AddWithValue("Username", username);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

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
