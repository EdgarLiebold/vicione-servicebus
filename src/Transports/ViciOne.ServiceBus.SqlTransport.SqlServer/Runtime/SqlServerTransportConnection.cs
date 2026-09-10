using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Owns a <see cref="SqlConnection" /> used by the SQL Server transport runtime.</summary>
internal sealed class SqlServerTransportConnection :
    ISqlServerTransportConnection
{
    /// <summary>Initializes the wrapper with a new SQL Server connection.</summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public SqlServerTransportConnection(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        Connection = new SqlConnection(connectionString);
    }

    /// <summary>Gets the underlying SQL Server connection.</summary>
    public SqlConnection Connection { get; }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        return Connection.DisposeAsync();
    }

    /// <summary>Opens the underlying SQL Server connection.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Connection.OpenAsync(cancellationToken);
    }

    /// <summary>Closes the underlying SQL Server connection.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Connection.CloseAsync();
    }

    /// <summary>Creates an administrative connection to the SQL Server <c>master</c> database.</summary>
    /// <param name="options">The transport and optional administrator credentials.</param>
    /// <returns>A connection configured for system-database migration operations.</returns>
    public static SqlServerTransportConnection GetSystemDatabaseConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        builder.InitialCatalog = "master";

        if (!string.IsNullOrWhiteSpace(options.AdminUsername))
            builder.UserID = options.AdminUsername;
        if (!string.IsNullOrWhiteSpace(options.AdminPassword))
            builder.Password = options.AdminPassword;

        return new SqlServerTransportConnection(builder.ToString());
    }

    /// <summary>Creates an administrative connection to the configured transport database.</summary>
    /// <param name="options">The transport and optional administrator credentials.</param>
    /// <returns>A connection configured for database migration operations.</returns>
    public static SqlServerTransportConnection GetDatabaseAdminConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        if (!string.IsNullOrWhiteSpace(options.AdminUsername))
            builder.UserID = options.AdminUsername;
        if (!string.IsNullOrWhiteSpace(options.AdminPassword))
            builder.Password = options.AdminPassword;

        return new SqlServerTransportConnection(builder.ToString());
    }

    /// <summary>Creates a connection to the configured transport database.</summary>
    /// <param name="options">The transport connection options.</param>
    /// <returns>A connection configured with the transport credentials.</returns>
    public static SqlServerTransportConnection GetDatabaseConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        return new SqlServerTransportConnection(builder.ToString());
    }

    /// <summary>Combines a connection string and explicit options into a new SQL Server connection-string builder.</summary>
    /// <param name="options">The options to project without modifying the source object.</param>
    /// <returns>The resulting SQL Server connection-string builder.</returns>
    public static SqlConnectionStringBuilder CreateBuilder(SqlTransportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new SqlConnectionStringBuilder(options.ConnectionString);

        if (!string.IsNullOrWhiteSpace(options.Host))
            builder.DataSource = FormatDataSource(options.Host, options.Port);

        if (!string.IsNullOrWhiteSpace(options.Database))
            builder.InitialCatalog = options.Database;

        if (!string.IsNullOrWhiteSpace(options.Username))
            builder.UserID = options.Username;
        if (!string.IsNullOrWhiteSpace(options.Password))
            builder.Password = options.Password;

        return builder;
    }

    static string FormatDataSource(string host, int? port)
    {
        return port.HasValue ? $"{host},{port.Value}" : host;
    }
}
