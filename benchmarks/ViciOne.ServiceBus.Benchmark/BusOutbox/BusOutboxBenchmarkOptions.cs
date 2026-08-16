namespace ViciOneServiceBusBenchmark.BusOutbox;

using System;
using NDesk.Options;


public class BusOutboxBenchmarkOptions :
    OptionSet
{
    public const string DefaultConnectionStringEnvironmentVariable = "VICIONE_BENCHMARK_SQLSERVER_CONNECTION_STRING";

    public BusOutboxBenchmarkOptions()
    {
        Add<long>("count:", "The number of messages to send", value => MessageCount = value);
        Add<ushort>("prefetch:", "The prefetch count for the broker", value => PrefetchCount = value);
        Add<int>("concurrency:", "The number of concurrent consumers", value => ConcurrencyLimit = value);
        Add<int>("clients:", "The number of sending message clients", value => Clients = value);
        Add<int>("payload:", "The size of the additional payload for the message", value => PayloadSize = value);
        Add<string>("outbox-db-connection-env:",
            "Environment variable containing the SQL Server connection string used by the bus outbox benchmark",
            value => ConnectionStringEnvironmentVariable = value);
        MessageCount = 10000;
        PrefetchCount = 100;
        Clients = 10;
        ConnectionStringEnvironmentVariable = DefaultConnectionStringEnvironmentVariable;
    }

    public int PayloadSize { get; set; }
    public long MessageCount { get; set; }
    public ushort PrefetchCount { get; set; }
    public int Clients { get; set; }
    public int? ConcurrencyLimit { get; set; }
    public string ConnectionStringEnvironmentVariable { get; private set; }

    public string ResolveDatabaseConnectionString()
    {
        return BusOutboxDatabaseSettings.ResolveConnectionString(ConnectionStringEnvironmentVariable);
    }
}
