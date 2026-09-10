using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Wraps an <see cref="NpgsqlConnection" /> for PostgreSQL SQL transport operations.</summary>
internal sealed class PostgreSqlTransportConnection :
    IPostgreSqlTransportConnection
{
    /// <summary>Initializes the wrapper with an existing PostgreSQL connection.</summary>
    /// <param name="connection">The connection owned by this wrapper.</param>
    public PostgreSqlTransportConnection(NpgsqlConnection connection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        return Connection.DisposeAsync();
    }

    /// <summary>Gets the underlying PostgreSQL connection.</summary>
    public NpgsqlConnection Connection { get; }

    /// <summary>Creates a command associated with the underlying connection.</summary>
    /// <param name="commandText">The SQL command text.</param>
    /// <returns>A command whose connection is set to <see cref="Connection" />.</returns>
    public NpgsqlCommand CreateCommand(string commandText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);

        var command = new NpgsqlCommand(commandText);
        command.Connection = Connection;

        return command;
    }

    /// <summary>Opens the underlying PostgreSQL connection.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Connection.OpenAsync(cancellationToken);
    }

    /// <summary>Closes the underlying PostgreSQL connection.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return Connection.CloseAsync();
    }

    /// <summary>Creates an administrative connection to the PostgreSQL <c>postgres</c> database.</summary>
    /// <param name="options">The transport and optional administrator credentials.</param>
    /// <returns>A connection configured for system-database migration operations.</returns>
    public static PostgreSqlTransportConnection GetSystemDatabaseConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        builder.Database = "postgres";

        if (!string.IsNullOrWhiteSpace(options.AdminUsername))
            builder.Username = options.AdminUsername;
        if (!string.IsNullOrWhiteSpace(options.AdminPassword))
            builder.Password = options.AdminPassword;

        return new PostgreSqlTransportConnection(new NpgsqlConnection(builder.ToString()));
    }

    /// <summary>Creates an administrative connection to the configured transport database.</summary>
    /// <param name="options">The transport and optional administrator credentials.</param>
    /// <returns>A connection configured for database migration operations.</returns>
    public static PostgreSqlTransportConnection GetDatabaseAdminConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        if (!string.IsNullOrWhiteSpace(options.AdminUsername))
            builder.Username = options.AdminUsername;
        if (!string.IsNullOrWhiteSpace(options.AdminPassword))
            builder.Password = options.AdminPassword;

        return new PostgreSqlTransportConnection(new NpgsqlConnection(builder.ToString()));
    }

    /// <summary>Creates a connection to the configured transport database.</summary>
    /// <param name="options">The transport connection options.</param>
    /// <returns>A connection configured with the transport credentials.</returns>
    public static PostgreSqlTransportConnection GetDatabaseConnection(SqlTransportOptions options)
    {
        return new PostgreSqlTransportConnection(new NpgsqlConnection(CreateBuilder(options).ToString()));
    }

    /// <summary>Combines a connection string and explicit options into a PostgreSQL connection-string builder.</summary>
    /// <param name="options">The options to apply. Missing connection fields and provider defaults are written back to this instance.</param>
    /// <returns>The resulting PostgreSQL connection-string builder.</returns>
    public static NpgsqlConnectionStringBuilder CreateBuilder(SqlTransportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

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

    /// <summary>Gets the PostgreSQL role name used for administrative migration grants.</summary>
    /// <param name="options">The options containing administrator or transport credentials.</param>
    /// <returns>The configured username without a server suffix, or <c>postgres</c> when no username is configured.</returns>
    public static string GetAdminMigrationPrincipal(SqlTransportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var principal = options.AdminUsername ?? options.Username ?? "postgres";

        return principal.Contains("@")
            ? principal.Substring(0, principal.IndexOf("@", StringComparison.Ordinal))
            : principal;
    }
}
