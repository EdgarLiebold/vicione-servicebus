using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Wraps a <see cref="SqlConnection" /> for SQL Server SQL transport operations.</summary>
public class SqlServerSqlTransportConnection :
    ISqlServerSqlTransportConnection
{
    /// <summary>Initializes the wrapper with a new SQL Server connection.</summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public SqlServerSqlTransportConnection(string connectionString)
    {
        Connection = new SqlConnection(connectionString);
    }

    /// <summary>Gets the underlying SQL Server connection.</summary>
    public SqlConnection Connection { get; }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        Connection.Dispose();

        return default;
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); Connection.Close();

        return Task.CompletedTask;
    }

    /// <summary>Creates an administrative connection to the SQL Server <c>master</c> database.</summary>
    /// <param name="options">The transport and optional administrator credentials.</param>
    /// <returns>A connection configured for system-database migration operations.</returns>
    public static SqlServerSqlTransportConnection GetSystemDatabaseConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        builder.InitialCatalog = "master";

        if (!string.IsNullOrWhiteSpace(options.AdminUsername))
            builder.UserID = options.AdminUsername;
        if (!string.IsNullOrWhiteSpace(options.AdminPassword))
            builder.Password = options.AdminPassword;

        return new SqlServerSqlTransportConnection(builder.ToString());
    }

    /// <summary>Creates an administrative connection to the configured transport database.</summary>
    /// <param name="options">The transport and optional administrator credentials.</param>
    /// <returns>A connection configured for database migration operations.</returns>
    public static SqlServerSqlTransportConnection GetDatabaseAdminConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        if (!string.IsNullOrWhiteSpace(options.AdminUsername))
            builder.UserID = options.AdminUsername;
        if (!string.IsNullOrWhiteSpace(options.AdminPassword))
            builder.Password = options.AdminPassword;

        return new SqlServerSqlTransportConnection(builder.ToString());
    }

    /// <summary>Creates a connection to the configured transport database.</summary>
    /// <param name="options">The transport connection options.</param>
    /// <returns>A connection configured with the transport credentials.</returns>
    public static SqlServerSqlTransportConnection GetDatabaseConnection(SqlTransportOptions options)
    {
        var builder = CreateBuilder(options);

        return new SqlServerSqlTransportConnection(builder.ToString());
    }

    /// <summary>Combines a connection string and explicit options into a SQL Server connection-string builder.</summary>
    /// <param name="options">The options to apply. Missing connection fields and provider defaults are written back to this instance.</param>
    /// <returns>The resulting SQL Server connection-string builder.</returns>
    public static SqlConnectionStringBuilder CreateBuilder(SqlTransportOptions options)
    {
        var builder = new SqlConnectionStringBuilder(options.ConnectionString) { TrustServerCertificate = true };

        if (!string.IsNullOrWhiteSpace(options.Host))
            builder.DataSource = options.FormatDataSource();
        else if (!string.IsNullOrWhiteSpace(builder.DataSource))
            (options.Host, options.Port) = ParseDataSource(builder.DataSource);

        if (!string.IsNullOrWhiteSpace(options.Database))
            builder.InitialCatalog = options.Database;
        else if (!string.IsNullOrWhiteSpace(builder.InitialCatalog))
            options.Database = builder.InitialCatalog;

        if (!string.IsNullOrWhiteSpace(options.Username))
            builder.UserID = options.Username;
        else if (!string.IsNullOrWhiteSpace(builder.UserID))
            options.Username = builder.UserID;
        if (!string.IsNullOrWhiteSpace(options.Password))
            builder.Password = options.Password;
        else if (!string.IsNullOrWhiteSpace(builder.Password))
            options.Password = builder.Password;

        if (string.IsNullOrWhiteSpace(options.Schema))
            options.Schema = "transport";

        if (string.IsNullOrWhiteSpace(options.Role))
            options.Role = "transport";

        return builder;
    }

    static (string? host, int? port) ParseDataSource(string? source)
    {
        var split = source?.Split(',');
        if (split?.Length == 2)
        {
            var host = split[0].Trim();

            if (int.TryParse(split[1].Trim(), out var port))
                return (host, port);

            return (host, null);
        }

        return (source?.Trim(), null);
    }
}
