namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;

using Microsoft.Data.SqlClient;

internal static class SqlServerTransportInspection
{
    public static async Task OpenWithin(
        this SqlConnection connection,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        await connection.OpenAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

    public static Task<long> Scalar(
        this SqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters) =>
        ScalarCore(connection, text, cancellationToken, parameters);

    public static Task<int> Execute(
        this SqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters) =>
        ExecuteCore(connection, text, cancellationToken, parameters);

    public static async Task<IReadOnlyList<string>> Strings(
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

    public static Task<IReadOnlyList<string>> SchemaTables(
        this SqlConnection connection,
        string schema,
        CancellationToken cancellationToken) =>
        connection.Strings(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = @schema AND table_type = 'BASE TABLE'",
            cancellationToken,
            ("schema", schema));

    public static Task<IReadOnlyList<string>> SchemaIndices(
        this SqlConnection connection,
        string schema,
        CancellationToken cancellationToken) =>
        connection.Strings(
            "SELECT i.name FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id "
            + "JOIN sys.schemas s ON s.schema_id = t.schema_id WHERE s.name = @schema AND i.name IS NOT NULL",
            cancellationToken,
            ("schema", schema));

    public static async Task<bool> DatabaseExists(
        this SqlConnection connection,
        string database,
        CancellationToken cancellationToken) =>
        await connection.Scalar(
            "SELECT COUNT(*) FROM sys.databases WHERE name = @database",
            cancellationToken,
            ("database", database)) > 0;

    public static async Task<bool> QueueExists(
        this SqlConnection connection,
        string schema,
        string queue,
        int type,
        CancellationToken cancellationToken) =>
        await connection.Scalar(
            $"SELECT COUNT(*) FROM [{schema}].[Queue] WHERE Name = @queue AND Type = @type",
            cancellationToken,
            ("queue", queue),
            ("type", type)) > 0;

    private static async Task<long> ScalarCore(
        SqlConnection connection,
        string text,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using SqlCommand command = connection.Command(text, parameters);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value);
    }

    private static async Task<int> ExecuteCore(
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
