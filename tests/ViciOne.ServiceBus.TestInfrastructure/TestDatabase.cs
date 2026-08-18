namespace ViciOne.ServiceBus.TestInfrastructure
{
    /// <summary>
    /// The databases the suites use, named in one place, and their connection strings taken from the
    /// runner contract. Nothing here probes for a server: an endpoint that was not published raises
    /// <see cref="TestRunnerContractException"/> from <see cref="TestRunnerContract"/>.
    /// </summary>
    public static class TestDatabase
    {
        /// <summary>The transport database the SQL transport specs share.</summary>
        public const string SqlTransport = "ViciOneServiceBus_transport_tests";

        /// <summary>
        /// The transport database of the provisioning specs. They create and drop a database on purpose,
        /// and dropping one disconnects every session still attached to it, so they must not be pointed at
        /// <see cref="SqlTransport"/>.
        /// </summary>
        public const string SqlTransportProvisioning = "ViciOneServiceBus_provisioning_tests";

        /// <summary>The database the persistence specs use.</summary>
        public const string Persistence = "ViciOneServiceBusUnitTests_v12_2015";

        /// <summary>The database of the PostgreSQL persistence specs.</summary>
        public const string PostgresPersistence = "ViciOneServiceBusUnitTests";

        /// <summary>SQL Server connection string of this run for the given database.</summary>
        public static string SqlServer(string database = Persistence)
        {
            return TestRunnerContract.SqlServer.SqlServerConnectionString(database);
        }

        /// <summary>SQL Server connection string of this run without a database.</summary>
        public static string SqlServerServer()
        {
            return TestRunnerContract.SqlServer.SqlServerConnectionString();
        }

        /// <summary>PostgreSQL connection string of this run for the given database.</summary>
        public static string Postgres(string database)
        {
            return TestRunnerContract.Postgres.PostgresConnectionString(database);
        }
    }
}
