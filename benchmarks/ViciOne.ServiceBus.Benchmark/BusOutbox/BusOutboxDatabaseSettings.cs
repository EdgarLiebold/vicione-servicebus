namespace ViciOneServiceBusBenchmark.BusOutbox;

using System;
using Microsoft.Data.SqlClient;
using NDesk.Options;


public static class BusOutboxDatabaseSettings
{
    public static string ResolveConnectionString(string environmentVariable)
    {
        if (string.IsNullOrWhiteSpace(environmentVariable))
            throw new OptionException("The bus outbox database environment variable name must not be empty.",
                "outbox-db-connection-env");

        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(value))
            throw new OptionException(
                $"Environment variable '{environmentVariable}' must contain an explicit SQL Server connection string.",
                "outbox-db-connection-env");

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(value);
        }
        catch (ArgumentException)
        {
            throw new OptionException(
                $"Environment variable '{environmentVariable}' does not contain a valid SQL Server connection string.",
                "outbox-db-connection-env");
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource) || string.IsNullOrWhiteSpace(builder.InitialCatalog))
            throw new OptionException(
                $"Environment variable '{environmentVariable}' must specify both Server and Initial Catalog.",
                "outbox-db-connection-env");

        if (builder.DataSource.Contains("(LocalDb)", StringComparison.OrdinalIgnoreCase))
            throw new OptionException("LocalDB is not supported. Configure a Linux-compatible SQL Server endpoint.",
                "outbox-db-connection-env");

        return builder.ConnectionString;
    }
}
