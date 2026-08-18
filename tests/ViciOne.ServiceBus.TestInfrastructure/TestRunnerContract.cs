namespace ViciOne.ServiceBus.TestInfrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// The one place a test reads the database fixtures the canonical runner started.
    /// <para>
    /// It is fail closed on purpose: no default, no probing and no fallback. A fixture that falls back
    /// to a fixed host, port and account either measures whatever else answers there or reports a
    /// connection refusal that looks like an infrastructure problem rather than a missing contract. An
    /// incomplete contract raises <see cref="TestRunnerContractException"/> naming the variables that
    /// are missing, before the first connection is attempted. The exception carries no secret.
    /// </para>
    /// </summary>
    public static class TestRunnerContract
    {
        public const string SqlServerHostVariable = "VICIONE_SERVICEBUS_MSSQL_HOST";
        public const string SqlServerPortVariable = "VICIONE_SERVICEBUS_MSSQL_PORT";
        public const string SqlServerUsernameVariable = "VICIONE_SERVICEBUS_MSSQL_USER";
        public const string SqlServerPasswordVariable = "VICIONE_SERVICEBUS_MSSQL_PASS";

        public const string PostgresHostVariable = "VICIONE_SERVICEBUS_PG_HOST";
        public const string PostgresPortVariable = "VICIONE_SERVICEBUS_PG_PORT";
        public const string PostgresUsernameVariable = "VICIONE_SERVICEBUS_PG_USER";
        public const string PostgresPasswordVariable = "VICIONE_SERVICEBUS_PG_PASS";

        static readonly Lazy<DatabaseEndpoint> _sqlServer = new(() => Read("SQL Server",
            SqlServerHostVariable, SqlServerPortVariable, SqlServerUsernameVariable, SqlServerPasswordVariable));

        static readonly Lazy<DatabaseEndpoint> _postgres = new(() => Read("PostgreSQL",
            PostgresHostVariable, PostgresPortVariable, PostgresUsernameVariable, PostgresPasswordVariable));

        /// <summary>The SQL Server fixture of this run. Throws when the runner published no complete endpoint.</summary>
        public static DatabaseEndpoint SqlServer => _sqlServer.Value;

        /// <summary>The PostgreSQL fixture of this run. Throws when the runner published no complete endpoint.</summary>
        public static DatabaseEndpoint Postgres => _postgres.Value;

        static DatabaseEndpoint Read(string engine, string hostVariable, string portVariable, string usernameVariable,
            string passwordVariable)
        {
            List<string> missing = Missing(hostVariable, portVariable, usernameVariable, passwordVariable);
            if (missing.Count > 0)
            {
                throw new TestRunnerContractException(
                    $"The {engine} fixture of this run is not described completely, so no connection is attempted. "
                    + $"Missing or unusable: {string.Join(", ", missing)}. "
                    + "Start the pinned fixture with tools/ci/run_broker_category.py, which publishes these variables. "
                    + "There is deliberately no default host, port, account or secret to fall back to.");
            }

            return new DatabaseEndpoint(engine, Value(hostVariable), Port(portVariable), Value(usernameVariable),
                Value(passwordVariable));
        }

        static List<string> Missing(string hostVariable, string portVariable, string usernameVariable, string passwordVariable)
        {
            var missing = new List<string>();

            foreach (var variable in new[] { hostVariable, usernameVariable, passwordVariable })
            {
                if (string.IsNullOrWhiteSpace(Value(variable)))
                    missing.Add(variable);
            }

            if (Port(portVariable) <= 0)
                missing.Add(portVariable);

            return missing.OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        static string Value(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }

        static int Port(string variable)
        {
            return int.TryParse(Value(variable), out var port) && port > 0 && port <= 65535 ? port : 0;
        }
    }
}
