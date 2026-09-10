using Microsoft.Data.SqlClient;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;

internal static class SqlServerTransportInspection
{
    internal sealed record ScheduledDelivery(DateTime EnqueueTimeUtc, DateTime DatabaseNowUtc);

    public static async Task OpenWithinAsync(
        this SqlConnection connection,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        await connection.OpenAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

    public static Task<long> ScalarAsync(
        this SqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters) =>
        ScalarCoreAsync(connection, text, cancellationToken, parameters);

    public static Task<int> ExecuteAsync(
        this SqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters) =>
        ExecuteCoreAsync(connection, text, cancellationToken, parameters);

    public static async Task<IReadOnlyList<string>> StringsAsync(
        this SqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using SqlCommand command = connection.Command(text, parameters);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var values = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            values.Add(reader.GetString(0).ToLowerInvariant());
        return values.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    public static Task<IReadOnlyList<string>> SchemaTablesAsync(
        this SqlConnection connection,
        string schema,
        CancellationToken cancellationToken) =>
        connection.StringsAsync(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = @schema AND table_type = 'BASE TABLE'",
            cancellationToken,
            ("schema", schema));

    public static Task<IReadOnlyList<string>> SchemaIndicesAsync(
        this SqlConnection connection,
        string schema,
        CancellationToken cancellationToken) =>
        connection.StringsAsync(
            "SELECT i.name FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id "
            + "JOIN sys.schemas s ON s.schema_id = t.schema_id WHERE s.name = @schema AND i.name IS NOT NULL",
            cancellationToken,
            ("schema", schema));

    public static Task<IReadOnlyList<string>> SchemaProceduresAsync(
        this SqlConnection connection,
        string schema,
        CancellationToken cancellationToken) =>
        connection.StringsAsync(
            "SELECT p.name FROM sys.procedures p JOIN sys.schemas s ON s.schema_id = p.schema_id WHERE s.name = @schema",
            cancellationToken,
            ("schema", schema));

    public static async Task<bool> DatabaseExistsAsync(
        this SqlConnection connection,
        string database,
        CancellationToken cancellationToken) =>
        await connection.ScalarAsync(
            "SELECT COUNT(*) FROM sys.databases WHERE name = @database",
            cancellationToken,
            ("database", database)) > 0;

    public static async Task<bool> QueueExistsAsync(
        this SqlConnection connection,
        string schema,
        string queue,
        int type,
        CancellationToken cancellationToken) =>
        await connection.ScalarAsync(
            $"SELECT COUNT(*) FROM [{schema}].[Queue] WHERE Name = @queue AND Type = @type",
            cancellationToken,
            ("queue", queue),
            ("type", type)) > 0;

    public static Task<long> DeliveryCountAsync(
        this SqlConnection connection,
        string schema,
        string queue,
        int queueType,
        CancellationToken cancellationToken) =>
        connection.ScalarAsync(
            $"SELECT COUNT(*) FROM [{schema}].[MessageDelivery] d "
            + $"JOIN [{schema}].[Queue] q ON q.Id = d.QueueId "
            + "WHERE q.Name = @queue AND q.Type = @type",
            cancellationToken,
            ("queue", queue),
            ("type", queueType));

    public static Task<long> MessageCountAsync(
        this SqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken) =>
        connection.ScalarAsync(
            $"SELECT COUNT(*) FROM [{schema}].[Message] WHERE MessageId = @messageId",
            cancellationToken,
            ("messageId", messageId));

    public static Task<long> DeliveryCountForMessageAsync(
        this SqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken) =>
        connection.ScalarAsync(
            $"SELECT COUNT(*) FROM [{schema}].[MessageDelivery] d "
            + $"JOIN [{schema}].[Message] m ON m.TransportMessageId = d.TransportMessageId "
            + "WHERE m.MessageId = @messageId",
            cancellationToken,
            ("messageId", messageId));

    public static async Task<string?> TransportHeadersForMessageAsync(
        this SqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.Command(
            $"SELECT d.TransportHeaders FROM [{schema}].[MessageDelivery] d "
            + $"JOIN [{schema}].[Message] m ON m.TransportMessageId = d.TransportMessageId "
            + "WHERE m.MessageId = @messageId",
            ("messageId", messageId));
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    public static Task<long> DeliveryAttemptForMessageAsync(
        this SqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken) =>
        connection.ScalarAsync(
            $"SELECT d.DeliveryCount FROM [{schema}].[MessageDelivery] d "
            + $"JOIN [{schema}].[Message] m ON m.TransportMessageId = d.TransportMessageId "
            + "WHERE m.MessageId = @messageId",
            cancellationToken,
            ("messageId", messageId));

    public static async Task<ScheduledDelivery> ScheduledDeliveryForMessageAsync(
        this SqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        string text = $"SELECT d.EnqueueTime, SYSUTCDATETIME() "
            + $"FROM [{schema}].[MessageDelivery] d "
            + $"JOIN [{schema}].[Message] m ON m.TransportMessageId = d.TransportMessageId "
            + "WHERE m.MessageId = @messageId";
        await using SqlCommand command = connection.Command(text, ("messageId", messageId));
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("The delayed delivery was not persisted by SQL Server.");
        var result = new ScheduledDelivery(
            DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc),
            DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc));
        if (await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("The message has more than one scheduled delivery.");
        return result;
    }

    private static async Task<long> ScalarCoreAsync(
        SqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using SqlCommand command = connection.Command(text, parameters);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value);
    }

    private static async Task<int> ExecuteCoreAsync(
        SqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using SqlCommand command = connection.Command(text, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SqlCommand Command(
        this SqlConnection connection,
        string text,
        params (string Name, object Value)[] parameters)
    {
        var command = new SqlCommand(text, connection);
        foreach ((string name, object value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return command;
    }
}
