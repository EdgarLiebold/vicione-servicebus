namespace ViciOne.ServiceBus.TestInfrastructure
{
    using System;


    /// <summary>
    /// One database fixture of this run, complete and unchangeable once read.
    /// <para>
    /// ToString names the engine and the endpoint and never the secret, so an endpoint may appear in a
    /// message, a log or an assertion without carrying a credential into it.
    /// </para>
    /// </summary>
    public sealed class DatabaseEndpoint
    {
        public DatabaseEndpoint(string engine, string host, int port, string username, string password)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));

            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), port, "The runner published no usable port.");

            Port = port;
        }

        public string Engine { get; }
        public string Host { get; }
        public int Port { get; }
        public string Username { get; }

        /// <summary>The secret of this run. It is never written to a log, a message or an assertion.</summary>
        public string Password { get; }

        /// <summary>SQL Server connection string for the given database.</summary>
        public string SqlServerConnectionString(string database)
        {
            return $"Server=tcp:{Host},{Port};Persist Security Info=False;User ID={Username};Password={Password};"
                + $"Initial Catalog={database};Encrypt=False;TrustServerCertificate=True;";
        }

        /// <summary>SQL Server connection string without a database, for server level work.</summary>
        public string SqlServerConnectionString()
        {
            return $"Server=tcp:{Host},{Port};Persist Security Info=False;User ID={Username};Password={Password};"
                + "Encrypt=False;TrustServerCertificate=True;";
        }

        /// <summary>PostgreSQL connection string for the given database.</summary>
        public string PostgresConnectionString(string database)
        {
            return $"host={Host};port={Port};user id={Username};password={Password};database={database};";
        }

        public override string ToString()
        {
            return $"{Engine} at {Host}:{Port} as {Username}";
        }
    }
}
