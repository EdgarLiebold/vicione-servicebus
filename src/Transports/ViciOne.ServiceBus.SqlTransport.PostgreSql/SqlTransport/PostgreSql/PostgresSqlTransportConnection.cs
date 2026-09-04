using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>
/// Provides a postgres sql transport connection implementation.
/// </summary>
public class PostgresSqlTransportConnection :
    IPostgresSqlTransportConnection
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    public PostgresSqlTransportConnection(NpgsqlConnection connection)
    {
        Connection = connection;
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return Connection.DisposeAsync();
    }

    /// <summary>
    /// Gets the connection value.
    /// </summary>
    public NpgsqlConnection Connection { get; }

    /// <summary>
    /// Creates command.
    /// </summary>
    /// <param name="commandText">The command text value.</param>
    /// <returns>The result of the operation.</returns>
    public NpgsqlCommand CreateCommand(string commandText)
    {
        var command = new NpgsqlCommand(commandText);
        command.Connection = Connection;

        return command;
    }

    /// <summary>
    /// Performs the open operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Connection.OpenAsync(cancellationToken);
    }

    /// <summary>
    /// Performs the close operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Connection.CloseAsync();
    }

    /// <summary>
    /// Gets system database connection.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static PostgresSqlTransportConnection GetSystemDatabaseConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        builder.Database = "postgres";

        if (!string.IsNullOrWhiteSpace(options.AdminUsername))
            builder.Username = options.AdminUsername;
        if (!string.IsNullOrWhiteSpace(options.AdminPassword))
            builder.Password = options.AdminPassword;

        return new PostgresSqlTransportConnection(new NpgsqlConnection(builder.ToString()));
    }

    /// <summary>
    /// Gets database admin connection.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static PostgresSqlTransportConnection GetDatabaseAdminConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        if (!string.IsNullOrWhiteSpace(options.AdminUsername))
            builder.Username = options.AdminUsername;
        if (!string.IsNullOrWhiteSpace(options.AdminPassword))
            builder.Password = options.AdminPassword;

        return new PostgresSqlTransportConnection(new NpgsqlConnection(builder.ToString()));
    }

    /// <summary>
    /// Gets database connection.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static PostgresSqlTransportConnection GetDatabaseConnection(SqlTransportOptions options)
    {
        return new PostgresSqlTransportConnection(new NpgsqlConnection(CreateBuilder(options).ToString()));
    }

    /// <summary>
    /// Creates builder.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static NpgsqlConnectionStringBuilder CreateBuilder(SqlTransportOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder(options.ConnectionString);

        if (!string.IsNullOrWhiteSpace(options.Host))
            builder.Host = options.Host;
        else if (!string.IsNullOrWhiteSpace(builder.Host))
            options.Host = builder.Host;

        if (!string.IsNullOrWhiteSpace(options.Database))
            builder.Database = options.Database;
        else if (!string.IsNullOrWhiteSpace(builder.Database))
            options.Database = builder.Database;

        if (!string.IsNullOrWhiteSpace(options.Username))
            builder.Username = options.Username;
        else if (!string.IsNullOrWhiteSpace(builder.Username))
            options.Username = builder.Username;

        if (!string.IsNullOrWhiteSpace(options.Password))
            builder.Password = options.Password;
        else if (!string.IsNullOrWhiteSpace(builder.Password))
            options.Password = builder.Password;

        if (options.Port.HasValue)
            builder.Port = options.Port.Value;
        else if (builder.Port != NpgsqlConnection.DefaultPort)
            options.Port = builder.Port;

        if (string.IsNullOrWhiteSpace(options.Schema))
            options.Schema = "transport";

        if (string.IsNullOrWhiteSpace(options.Role))
            options.Role = "transport";

        return builder;
    }

    /// <summary>
    /// Gets admin migration principal.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static string? GetAdminMigrationPrincipal(SqlTransportOptions options)
    {
        var principal = options.AdminUsername ?? options.Username ?? "postgres";

        return principal.Contains("@")
            ? principal.Substring(0, principal.IndexOf("@", StringComparison.Ordinal))
            : principal;
    }
}
