namespace ViciOne.ServiceBus.DbTransport.Tests;

using ViciOne.ServiceBus.Tests;


/// <summary>
/// Points the SQL transport specs at the database fixture the canonical runner started.
/// <para>
/// The imported baseline wrote the endpoint straight into the two test configurations: host
/// 'localhost' on the engine default port, with the well known administrative secret. The runner
/// publishes an ephemeral loopback port per run instead, so those specs could only ever reach a
/// server that happened to sit on the default port — on a machine without one they report
/// connection refused, and on a machine with an unrelated one they measure the wrong server.
/// That is the same defect <see cref="RunScopedDatabase"/> already closed for the persistence specs.
/// </para>
/// <para>
/// The run-scoped fixture wins and is not probed. The historic literals stay as the documented
/// fallback for a developer running against their own server, which is the same policy
/// <see cref="LocalDbConnectionStringProvider"/> follows.
/// </para>
/// </summary>
public static class RunScopedTransportEndpoint
{
    const string FallbackHost = "localhost";
    const string FallbackAdminPassword = "Password12!";

    /// <summary>The transport database every spec shares.</summary>
    public const string SharedDatabase = "ViciOneServiceBus_transport_tests";

    /// <summary>
    /// The transport database of the provisioning specs. They create and drop a database on purpose,
    /// and dropping one disconnects every session still attached to it, so they must not be pointed
    /// at <see cref="SharedDatabase"/>.
    /// </summary>
    public const string ProvisioningDatabase = "ViciOneServiceBus_provisioning_tests";

    /// <summary>
    /// PostgreSQL connection string of the run-scoped fixture, or the historic default when the
    /// runner started none. Specs that derive their whole configuration from a connection string
    /// read it here instead of carrying their own literal.
    /// </summary>
    public static string PostgresConnectionString(string database)
    {
        return RunScopedDatabase.PostgresIsConfigured
            ? RunScopedDatabase.PostgresConnectionString(database)
            : $"host={FallbackHost};user id=postgres;password={FallbackAdminPassword};database={database};";
    }

    /// <summary>Applies the run-scoped PostgreSQL endpoint, keeping the historic values as fallback.</summary>
    public static void UseRunScopedPostgres(this SqlTransportOptions options)
    {
        var configured = RunScopedDatabase.PostgresIsConfigured;

        options.Host = configured ? RunScopedDatabase.PostgresHost : FallbackHost;
        options.Port = configured ? RunScopedDatabase.PostgresPort : null;
        options.AdminUsername = configured ? RunScopedDatabase.PostgresUsername : "postgres";
        options.AdminPassword = configured ? RunScopedDatabase.PostgresPassword : FallbackAdminPassword;
    }

    /// <summary>Applies the run-scoped SQL Server endpoint, keeping the historic values as fallback.</summary>
    public static void UseRunScopedSqlServer(this SqlTransportOptions options)
    {
        var configured = RunScopedDatabase.SqlServerIsConfigured;

        options.Host = configured ? RunScopedDatabase.SqlServerHost : FallbackHost;
        options.Port = configured ? RunScopedDatabase.SqlServerPort : null;
        options.AdminUsername = RunScopedDatabase.SqlServerAccount;
        options.AdminPassword = configured ? RunScopedDatabase.SqlServerPassword : FallbackAdminPassword;
    }
}
