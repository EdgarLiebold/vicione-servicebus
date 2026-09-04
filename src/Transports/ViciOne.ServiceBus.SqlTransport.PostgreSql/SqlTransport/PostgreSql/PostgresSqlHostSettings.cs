using System;
using System.Linq;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>
/// Provides a postgres sql host settings implementation.
/// </summary>
public class PostgresSqlHostSettings :
    ConfigurationSqlHostSettings
{
    readonly NpgsqlDataSource? _dataSource;
    NpgsqlConnectionStringBuilder? _builder;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    public PostgresSqlHostSettings(Uri hostAddress)
        : base(hostAddress)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionString">The connection string value.</param>
    public PostgresSqlHostSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="dataSource">The data source value.</param>
    public PostgresSqlHostSettings(NpgsqlDataSource dataSource)
    {
        if (dataSource == null)
            throw new ArgumentNullException(nameof(dataSource));

        _dataSource = dataSource;

        IsProvidedDataSource = true;

        ConnectionString = dataSource.ConnectionString;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public PostgresSqlHostSettings(SqlTransportOptions options)
    {
        var builder = PostgresSqlTransportConnection.CreateBuilder(options);

        ParseHost(builder.Host);
        if (builder.Port > 0 && builder.Port != NpgsqlConnection.DefaultPort)
            Port = options.Port;

        Database = builder.Database;
        Schema = options.Schema;

        Username = builder.Username;
        Password = builder.Password;

        _builder = builder;

        if (options.ConnectionLimit.HasValue)
            ConnectionLimit = options.ConnectionLimit.Value;

        MaintenanceEnabled = !options.DisableMaintenance;
    }

    /// <summary>
    /// Gets or sets the multiple hosts value.
    /// </summary>
    public string? MultipleHosts { get; set; }

    /// <summary>
    /// If true, the data source was provided by the developer and should not be disposed
    /// </summary>
    public bool IsProvidedDataSource { get; private set; }

    /// <summary>
    /// Gets or sets the connection string value.
    /// </summary>
    public string? ConnectionString
    {
        set
        {
            var builder = new NpgsqlConnectionStringBuilder(value);

            ParseHost(builder.Host);
            if (builder.Port > 0 && builder.Port != NpgsqlConnection.DefaultPort)
                Port = builder.Port;

            Database = builder.Database;

            Username = builder.Username;
            Password = builder.Password;

            Schema = builder.SearchPath ?? "transport";

            _builder = builder;
        }
    }

    /// <summary>
    /// Gets data source.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public NpgsqlDataSource GetDataSource()
    {
        if (_dataSource != null)
            return _dataSource;

        var builder = _builder ??= new NpgsqlConnectionStringBuilder
        {
            Host = MultipleHosts ?? Host,
            Username = Username,
            Password = Password,
            Database = Database
        };

        if (Port.HasValue && Port.Value != NpgsqlConnection.DefaultPort)
            builder.Port = Port.Value;

        return NpgsqlDataSource.Create(builder);
    }

    /// <summary>
    /// Creates connection context factory.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <returns>The result of the operation.</returns>
    public override ConnectionContextFactory CreateConnectionContextFactory(ISqlHostConfiguration hostConfiguration)
    {
        return new PostgresConnectionContextFactory(hostConfiguration);
    }

    void ParseHost(string? host)
    {
        var hostSegments = host?.Split(',');
        if (hostSegments?.Length > 1)
        {
            Host = hostSegments[0].Split(':').First().Trim();
            MultipleHosts = host!.Trim();
        }
        else
        {
            var segments = host?.Split(':');
            if (segments?.Length == 1)
                Host = segments[0].Trim();
            else if (segments?.Length == 2)
            {
                Host = segments[0].Trim();

                if (int.TryParse(segments[1], out var port) && port != 0 && port != NpgsqlConnection.DefaultPort)
                    Port = port;
            }
        }
    }
}
