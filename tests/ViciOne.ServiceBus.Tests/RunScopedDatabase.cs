// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-09.
namespace ViciOne.ServiceBus.Tests
{
    using System;


    /// <summary>
    /// Single place where a spec reads the database fixtures the canonical runner started.
    /// <para>
    /// The imported baseline wrote host, port and account straight into the specs: localhost:1433 with
    /// 'sa' and localhost:5432 with 'postgres', both carrying the same well known password. That is the
    /// defect that already cost this work package fourteen RabbitMQ tests, which were green only because
    /// an unrelated container happened to hold the default port. The fixtures publish an ephemeral
    /// loopback port per run, so every value comes from the environment instead.
    /// </para>
    /// <para>
    /// SQL Server cannot rename its 'sa' account, so there the name is fixed and only the secret is
    /// run-scoped. PostgreSQL takes both from the run.
    /// </para>
    /// </summary>
    public static class RunScopedDatabase
    {
        public const string SqlServerHostVariable = "VICIONE_SERVICEBUS_MSSQL_HOST";
        public const string SqlServerPortVariable = "VICIONE_SERVICEBUS_MSSQL_PORT";
        public const string SqlServerPasswordVariable = "VICIONE_SERVICEBUS_MSSQL_PASS";

        public const string PostgresHostVariable = "VICIONE_SERVICEBUS_PG_HOST";
        public const string PostgresPortVariable = "VICIONE_SERVICEBUS_PG_PORT";
        public const string PostgresUsernameVariable = "VICIONE_SERVICEBUS_PG_USER";
        public const string PostgresPasswordVariable = "VICIONE_SERVICEBUS_PG_PASS";

        /// <summary>SQL Server cannot rename this account, so only its secret is run-scoped.</summary>
        public const string SqlServerAccount = "sa";

        static string Value(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }

        static int Port(string variable)
        {
            return int.TryParse(Value(variable), out var port) && port > 0 ? port : 0;
        }

        /// <summary>True when the runner started a SQL Server fixture and published its endpoint.</summary>
        public static bool SqlServerIsConfigured =>
            !string.IsNullOrWhiteSpace(Value(SqlServerHostVariable)) && Port(SqlServerPortVariable) > 0
            && !string.IsNullOrWhiteSpace(Value(SqlServerPasswordVariable));

        /// <summary>True when the runner started a PostgreSQL fixture and published its endpoint.</summary>
        public static bool PostgresIsConfigured =>
            !string.IsNullOrWhiteSpace(Value(PostgresHostVariable)) && Port(PostgresPortVariable) > 0
            && !string.IsNullOrWhiteSpace(Value(PostgresPasswordVariable));

        /// <summary>Connection string of the run-scoped SQL Server, without a database name.</summary>
        public static string SqlServerConnectionStringPrefix =>
            $"Server=tcp:{Value(SqlServerHostVariable)},{Port(SqlServerPortVariable)};"
            + $"Persist Security Info=False;User ID={SqlServerAccount};Password={Value(SqlServerPasswordVariable)};"
            + "Encrypt=False;TrustServerCertificate=True;";

        /// <summary>Connection string of the run-scoped PostgreSQL for the given database.</summary>
        public static string PostgresConnectionString(string database)
        {
            return $"host={Value(PostgresHostVariable)};port={Port(PostgresPortVariable)};"
                + $"user id={Value(PostgresUsernameVariable)};password={Value(PostgresPasswordVariable)};"
                + $"database={database};";
        }
    }
}
