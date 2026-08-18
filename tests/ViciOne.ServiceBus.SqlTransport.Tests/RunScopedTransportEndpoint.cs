namespace ViciOne.ServiceBus.DbTransport.Tests;

using ViciOne.ServiceBus.TestInfrastructure;


/// <summary>
/// Points the SQL transport specs at the database fixtures of this run.
/// <para>
/// The imported baseline wrote the endpoint into the two test configurations: host 'localhost' on the
/// engine default port with the well known administrative secret. The runner publishes an ephemeral
/// loopback port per run, so those specs could only ever reach a server that happened to sit on the
/// default port - on a machine without one they reported a refused connection, and on a machine with an
/// unrelated one they measured the wrong server. Both were silent about which of the two had happened.
/// </para>
/// <para>
/// There is therefore no fallback. Every value comes from <see cref="TestRunnerContract"/>, and an
/// endpoint the runner did not publish raises <see cref="TestRunnerContractException"/> before the first
/// connection attempt.
/// </para>
/// </summary>
public static class RunScopedTransportEndpoint
{
    /// <summary>The transport database every spec shares.</summary>
    public const string SharedDatabase = TestDatabase.SqlTransport;

    /// <summary>The transport database of the provisioning specs, which create and drop one on purpose.</summary>
    public const string ProvisioningDatabase = TestDatabase.SqlTransportProvisioning;

    /// <summary>PostgreSQL connection string of this run for the given database.</summary>
    public static string PostgresConnectionString(string database)
    {
        return TestDatabase.Postgres(database);
    }

    /// <summary>SQL Server connection string of this run for the given database.</summary>
    public static string SqlServerConnectionString(string database)
    {
        return TestDatabase.SqlServer(database);
    }

    /// <summary>Applies the PostgreSQL endpoint of this run.</summary>
    public static void UseRunScopedPostgres(this SqlTransportOptions options)
    {
        var endpoint = TestRunnerContract.Postgres;

        options.Host = endpoint.Host;
        options.Port = endpoint.Port;
        options.AdminUsername = endpoint.Username;
        options.AdminPassword = endpoint.Password;
    }

    /// <summary>Applies the SQL Server endpoint of this run.</summary>
    public static void UseRunScopedSqlServer(this SqlTransportOptions options)
    {
        var endpoint = TestRunnerContract.SqlServer;

        options.Host = endpoint.Host;
        options.Port = endpoint.Port;
        options.AdminUsername = endpoint.Username;
        options.AdminPassword = endpoint.Password;
    }
}
