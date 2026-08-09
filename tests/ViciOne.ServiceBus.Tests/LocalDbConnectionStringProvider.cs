// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Data.SqlClient;


    public static class LocalDbConnectionStringProvider
    {
        /// <summary>
        /// Candidate connection strings, tried in order. The run-scoped fixture comes first, so a
        /// developer machine that also happens to run a SQL Server on the default port cannot be
        /// measured by accident. The remaining entries are the historic ones from the imported
        /// baseline and stay as a fallback for someone running against their own server.
        /// </summary>
        static readonly string[] _possibleLocalDbConnectionStrings =
        {
            @"Data Source=(LocalDb)\MSSQLLocalDB;Integrated Security=True;", // the localdb installed with VS 2015
            @"Data Source=(LocalDb)\ProjectsV12;Integrated Security=True;", // the localdb with VS 2013
            @"Data Source=(LocalDb)\v11.0;Integrated Security=True;" // the older version of localdb
        };

        static readonly object _lockConnectionString = new object();
        static string _connectionString;

        /// <summary>
        /// Loops through the array of potential localdb connection strings to find one that we can use for the unit tests
        /// </summary>
        public static string GetLocalDbConnectionString(string initialCatalog = "ViciOneServiceBusUnitTests_v12_2015;")
        {
            if (!string.IsNullOrWhiteSpace(_connectionString))
                return _connectionString + "Initial Catalog=" + initialCatalog;

            var exceptions = new List<Exception>();
            lock (_lockConnectionString)
            {
                if (!string.IsNullOrWhiteSpace(_connectionString))
                    return _connectionString + "Initial Catalog=" + initialCatalog;

                // The fixture the runner started wins, and it is not probed: if it is configured it is
                // the only server these specs may measure.
                if (RunScopedDatabase.SqlServerIsConfigured)
                {
                    _connectionString = RunScopedDatabase.SqlServerConnectionStringPrefix;
                    return _connectionString + "Initial Catalog=" + initialCatalog;
                }

                // Lets find a localdb that we can use for our unit test
                foreach (var connectionString in _possibleLocalDbConnectionStrings)
                {
                    try
                    {
                        using var connection = new SqlConnection(connectionString);
                        connection.Open();

                        // It worked, we can save this as our connection string
                        _connectionString = connectionString;
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Swallow
                        exceptions.Add(ex);
                    }
                }

                // If we looped through all possible localdb connection strings and didn't find one, we fail.
                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    exceptions.Insert(0, new InvalidOperationException(
                        $"No SQL Server available. Either start the pinned fixture, which publishes "
                        + $"{RunScopedDatabase.SqlServerHostVariable} and {RunScopedDatabase.SqlServerPortVariable}, "
                        + "or install one of the listed LocalDB versions."));
                    throw new AggregateException(exceptions);
                }
            }

            return _connectionString + "Initial Catalog=" + initialCatalog;
        }
    }
}
