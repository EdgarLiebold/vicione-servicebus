using Npgsql;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;

internal static class PostgreSqlTransportInspection
{
    internal sealed record ScheduledDelivery(DateTime EnqueueTimeUtc, DateTime DatabaseNowUtc);

    public static async Task<IReadOnlyList<string>> SchemaTables(
        this NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string text =
            "SELECT table_name FROM information_schema.tables "
            + "WHERE table_schema = @schema AND table_type = 'BASE TABLE'";
        return await connection.Strings(text, cancellationToken, ("schema", schema));
    }

    public static async Task<IReadOnlyList<string>> SchemaIndices(
        this NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string text = "SELECT indexname FROM pg_indexes WHERE schemaname = @schema";
        return await connection.Strings(text, cancellationToken, ("schema", schema));
    }

    public static Task<long> DeliveryCount(
        this NpgsqlConnection connection,
        string schema,
        string queue,
        int queueType,
        CancellationToken cancellationToken) =>
        connection.Scalar(
            $"SELECT COUNT(*) FROM \"{schema}\".message_delivery d "
            + $"JOIN \"{schema}\".queue q ON q.id = d.queue_id "
            + "WHERE q.name = @queue AND q.type = @type",
            cancellationToken,
            ("queue", queue),
            ("type", queueType));

    public static Task<long> MessageCount(
        this NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken) =>
        connection.Scalar(
            $"SELECT COUNT(*) FROM \"{schema}\".message WHERE message_id = @messageId",
            cancellationToken,
            ("messageId", messageId));

    public static Task<long> DeliveryCountForMessage(
        this NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken) =>
        connection.Scalar(
            $"SELECT COUNT(*) FROM \"{schema}\".message_delivery d "
            + $"JOIN \"{schema}\".message m ON m.transport_message_id = d.transport_message_id "
            + "WHERE m.message_id = @messageId",
            cancellationToken,
            ("messageId", messageId));

    public static async Task<string?> TransportHeadersForMessage(
        this NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = connection.Command(
            $"SELECT d.transport_headers::text FROM \"{schema}\".message_delivery d "
            + $"JOIN \"{schema}\".message m ON m.transport_message_id = d.transport_message_id "
            + "WHERE m.message_id = @messageId",
            ("messageId", messageId));
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    public static Task<long> DeliveryAttemptForMessage(
        this NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken) =>
        connection.Scalar(
            $"SELECT d.delivery_count FROM \"{schema}\".message_delivery d "
            + $"JOIN \"{schema}\".message m ON m.transport_message_id = d.transport_message_id "
            + "WHERE m.message_id = @messageId",
            cancellationToken,
            ("messageId", messageId));

    public static async Task<ScheduledDelivery> ScheduledDeliveryForMessage(
        this NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        string text = $"SELECT d.enqueue_time, clock_timestamp() "
            + $"FROM \"{schema}\".message_delivery d "
            + $"JOIN \"{schema}\".message m ON m.transport_message_id = d.transport_message_id "
            + "WHERE m.message_id = @messageId";
        await using NpgsqlCommand command = connection.Command(text, ("messageId", messageId));
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken), "The delayed delivery was not persisted by PostgreSQL.");
        var result = new ScheduledDelivery(reader.GetDateTime(0), reader.GetDateTime(1));
        Assert.False(await reader.ReadAsync(cancellationToken), "The message has more than one scheduled delivery.");
        return result;
    }

    public static async Task<bool> DatabaseExists(
        this NpgsqlConnection connection,
        string database,
        CancellationToken cancellationToken) =>
        await connection.Scalar(
            "SELECT COUNT(*) FROM pg_database WHERE datname = @database",
            cancellationToken,
            ("database", database)) > 0;

    public static async Task OpenWithin(
        this NpgsqlConnection connection,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await connection.OpenAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
    }

    private static async Task<IReadOnlyList<string>> Strings(
        this NpgsqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using NpgsqlCommand command = connection.Command(text, parameters);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var values = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            values.Add(reader.GetString(0).ToLowerInvariant());
        return values.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static async Task<long> Scalar(
        this NpgsqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using NpgsqlCommand command = connection.Command(text, parameters);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value);
    }

    private static NpgsqlCommand Command(
        this NpgsqlConnection connection,
        string text,
        params (string Name, object Value)[] parameters)
    {
        var command = new NpgsqlCommand(text, connection);
        foreach ((string name, object value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return command;
    }
}
