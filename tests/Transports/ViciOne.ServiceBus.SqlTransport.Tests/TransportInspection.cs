namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using SqlTransport.PostgreSql;
using SqlTransport.SqlServer;


/// <summary>Which engine a fixture is running against. The two migrators name their objects differently.</summary>
public enum TransportDialect
{
    Postgres,
    SqlServer
}


/// <summary>
/// Reads the transport's own database state, so a fixture can assert what the transport actually did
/// instead of asserting that it opened a connection or that some time passed. Every statement here is
/// read only apart from the two that a fixture uses to arrange state it could not otherwise reach.
/// </summary>
public static class TransportInspection
{
    public static TransportDialect DialectOf(IDatabaseTestConfiguration configuration)
    {
        return configuration is SqlServerDatabaseTestConfiguration ? TransportDialect.SqlServer : TransportDialect.Postgres;
    }

    public static async Task<DbConnection> OpenTransport(this IServiceProvider provider, TransportDialect dialect)
    {
        var options = provider.GetRequiredService<IOptions<SqlTransportOptions>>().Value;

        DbConnection connection = dialect == TransportDialect.SqlServer
            ? SqlServerSqlTransportConnection.GetDatabaseConnection(options).Connection
            : PostgresSqlTransportConnection.GetDatabaseConnection(options).Connection;

        await connection.OpenAsync();

        return connection;
    }

    public static async Task<DbConnection> OpenServer(this IServiceProvider provider, TransportDialect dialect)
    {
        var options = provider.GetRequiredService<IOptions<SqlTransportOptions>>().Value;

        DbConnection connection = dialect == TransportDialect.SqlServer
            ? SqlServerSqlTransportConnection.GetSystemDatabaseConnection(options).Connection
            : PostgresSqlTransportConnection.GetSystemDatabaseConnection(options).Connection;

        await connection.OpenAsync();

        return connection;
    }

    /// <summary>The tables the migrator created in the transport schema, lower cased and sorted.</summary>
    public static Task<IReadOnlyList<string>> SchemaTables(this DbConnection connection, string schema)
    {
        return connection.Strings(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = @schema",
            ("schema", schema));
    }

    /// <summary>The indices the migrator created in the transport schema, lower cased and sorted.</summary>
    public static Task<IReadOnlyList<string>> SchemaIndices(this DbConnection connection, TransportDialect dialect, string schema)
    {
        return dialect == TransportDialect.SqlServer
            ? connection.Strings(
                "SELECT i.name FROM sys.indexes i "
                + "JOIN sys.tables t ON t.object_id = i.object_id "
                + "JOIN sys.schemas s ON s.schema_id = t.schema_id "
                + "WHERE s.name = @schema AND i.name IS NOT NULL",
                ("schema", schema))
            : connection.Strings(
                "SELECT indexname FROM pg_indexes WHERE schemaname = @schema",
                ("schema", schema));
    }

    /// <summary>Whether a database of that name exists on the server this connection is attached to.</summary>
    public static async Task<bool> DatabaseExists(this DbConnection connection, TransportDialect dialect, string database)
    {
        var text = dialect == TransportDialect.SqlServer
            ? "SELECT COUNT(*) FROM sys.databases WHERE name = @database"
            : "SELECT COUNT(*) FROM pg_database WHERE datname = @database";

        return await connection.Scalar(text, ("database", database)) > 0;
    }

    /// <summary>The delivery count limit the migrator wrote for that queue, which is what the configurator sets.</summary>
    public static Task<long> QueueMaxDeliveryCount(this DbConnection connection, TransportDialect dialect, string schema, string queue)
    {
        var text = dialect == TransportDialect.SqlServer
            ? $"SELECT MaxDeliveryCount FROM {schema}.Queue WHERE Name = @queue AND Type = 1"
            : $"SELECT max_delivery_count FROM \"{schema}\".queue WHERE name = @queue AND type = 1";

        return connection.Scalar(text, ("queue", queue));
    }

    /// <summary>How many deliveries sit in that queue. Type 1 is the queue itself, type 3 its dead letter queue.</summary>
    public static Task<long> DeliveryCount(this DbConnection connection, TransportDialect dialect, string schema, string queue, int queueType)
    {
        var text = dialect == TransportDialect.SqlServer
            ? $"SELECT COUNT(*) FROM {schema}.MessageDelivery d JOIN {schema}.Queue q ON q.Id = d.QueueId "
              + "WHERE q.Name = @queue AND q.Type = @type"
            : $"SELECT COUNT(*) FROM \"{schema}\".message_delivery d JOIN \"{schema}\".queue q ON q.id = d.queue_id "
              + "WHERE q.name = @queue AND q.type = @type";

        return connection.Scalar(text, ("queue", queue), ("type", queueType));
    }

    /// <summary>
    /// When the lock on the single delivery of that queue expires. The transport pushes this forward every
    /// time it renews, so an expiry that has moved past the moment the lock was first taken is the renewal
    /// itself rather than a consequence of it.
    /// </summary>
    public static async Task<DateTime?> LockExpiry(this DbConnection connection, TransportDialect dialect, string schema, string queue)
    {
        var text = dialect == TransportDialect.SqlServer
            ? $"SELECT TOP 1 d.EnqueueTime FROM {schema}.MessageDelivery d JOIN {schema}.Queue q ON q.Id = d.QueueId "
              + "WHERE q.Name = @queue AND q.Type = 1"
            : $"SELECT d.enqueue_time FROM \"{schema}\".message_delivery d JOIN \"{schema}\".queue q ON q.id = d.queue_id "
              + "WHERE q.name = @queue AND q.type = 1 LIMIT 1";

        await using var command = connection.Command(text, new[] { ("queue", (object)queue) });

        var value = await command.ExecuteScalarAsync();

        return value == null || value == DBNull.Value ? null : DateTime.SpecifyKind(Convert.ToDateTime(value), DateTimeKind.Utc);
    }

    /// <summary>
    /// Drives every delivery of that queue to its limit. The transport refuses to fetch a delivery whose
    /// count has reached the maximum and moves it to the dead letter queue, which is the behaviour the
    /// limit exists for; arranging the count is the only way to reach that boundary without waiting for
    /// as many lock expiries as the limit allows.
    /// </summary>
    public static Task<int> ExhaustDeliveryAttempts(this DbConnection connection, TransportDialect dialect, string schema, string queue)
    {
        var text = dialect == TransportDialect.SqlServer
            ? $"UPDATE d SET d.DeliveryCount = d.MaxDeliveryCount FROM {schema}.MessageDelivery d "
              + $"JOIN {schema}.Queue q ON q.Id = d.QueueId WHERE q.Name = @queue AND q.Type = 1"
            : $"UPDATE \"{schema}\".message_delivery d SET delivery_count = d.max_delivery_count "
              + $"FROM \"{schema}\".queue q WHERE q.id = d.queue_id AND q.name = @queue AND q.type = 1";

        return connection.Execute(text, ("queue", queue));
    }

    /// <summary>
    /// Whether the transport holds a queue of that name and type. Type 1 is the queue itself, 2 its
    /// error queue and 3 its dead letter queue.
    /// </summary>
    public static async Task<bool> QueueExists(this DbConnection connection, TransportDialect dialect, string schema,
        string queue, int queueType)
    {
        var text = dialect == TransportDialect.SqlServer
            ? $"SELECT COUNT(*) FROM {schema}.Queue WHERE Name = @queue AND Type = @type"
            : $"SELECT COUNT(*) FROM \"{schema}\".queue WHERE name = @queue AND type = @type";

        return await connection.Scalar(text, ("queue", queue), ("type", queueType)) > 0;
    }

    /// <summary>
    /// How many deliveries sit in queues of that type, across every queue name. A fixture that lets the
    /// transport name its endpoints cannot ask for one queue, so it compares this before and after.
    /// </summary>
    public static Task<long> DeliveryCountByQueueType(this DbConnection connection, TransportDialect dialect,
        string schema, int queueType)
    {
        var text = dialect == TransportDialect.SqlServer
            ? $"SELECT COUNT(*) FROM {schema}.MessageDelivery d JOIN {schema}.Queue q ON q.Id = d.QueueId "
              + "WHERE q.Type = @type"
            : $"SELECT COUNT(*) FROM \"{schema}\".message_delivery d JOIN \"{schema}\".queue q ON q.id = d.queue_id "
              + "WHERE q.type = @type";

        return connection.Scalar(text, ("type", queueType));
    }

    /// <summary>How many messages the transport holds in total, across every queue.</summary>
    public static Task<long> MessageCount(this DbConnection connection, TransportDialect dialect, string schema)
    {
        var text = dialect == TransportDialect.SqlServer
            ? $"SELECT COUNT(*) FROM {schema}.Message"
            : $"SELECT COUNT(*) FROM \"{schema}\".message";

        return connection.Scalar(text);
    }

    static async Task<IReadOnlyList<string>> Strings(this DbConnection connection, string text, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.Command(text, parameters);
        await using var reader = await command.ExecuteReaderAsync();

        var values = new List<string>();
        while (await reader.ReadAsync())
            values.Add(reader.GetString(0).ToLowerInvariant());

        return values.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    static async Task<long> Scalar(this DbConnection connection, string text, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.Command(text, parameters);

        var value = await command.ExecuteScalarAsync();

        return value == null || value == DBNull.Value ? 0 : Convert.ToInt64(value);
    }

    static async Task<int> Execute(this DbConnection connection, string text, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.Command(text, parameters);

        return await command.ExecuteNonQueryAsync();
    }

    static DbCommand Command(this DbConnection connection, string text, (string Name, object Value)[] parameters)
    {
        // Npgsql and SqlClient spell a parameter the same way in the statement text, so the statements
        // above differ only where the two schemas genuinely differ.
        DbCommand command = connection is SqlConnection sqlServer
            ? new SqlCommand(text, sqlServer)
            : new NpgsqlCommand(text, (NpgsqlConnection)connection);

        command.CommandText = text;

        foreach ((var name, var value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@" + name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        return command;
    }
}
