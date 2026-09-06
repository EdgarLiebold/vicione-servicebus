using System;
using System.Linq;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Defines PostgreSQL connection and maintenance settings for a SQL transport host.</summary>
public class PostgresSqlHostSettings :
    ConfigurationSqlHostSettings
{
    readonly NpgsqlDataSource? _dataSource;
    NpgsqlConnectionStringBuilder? _builder;

    /// <summary>Initializes the settings from a PostgreSQL host address.</summary>
    /// <param name="hostAddress">The PostgreSQL host address.</param>
    public PostgresSqlHostSettings(Uri hostAddress)
        : base(hostAddress)
    {
    }

    /// <summary>Initializes the settings from a PostgreSQL connection string.</summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public PostgresSqlHostSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>Initializes the settings with a caller-owned PostgreSQL data source.</summary>
    /// <param name="dataSource">The preconfigured data source used to open connections.</param>
    public PostgresSqlHostSettings(NpgsqlDataSource dataSource)
    {
        if (dataSource == null)
            throw new ArgumentNullException(nameof(dataSource));

        _dataSource = dataSource;

        IsProvidedDataSource = true;

        ConnectionString = dataSource.ConnectionString;
    }

    /// <summary>Initializes the settings from SQL transport options.</summary>
    /// <param name="options">The SQL transport options.</param>
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

    /// <summary>Gets or sets the comma-separated PostgreSQL host list, including any per-host ports.</summary>
    public string? MultipleHosts { get; set; }

    /// <summary>Gets whether the data source was supplied by the caller and therefore is not owned by the transport.</summary>
    public bool IsProvidedDataSource { get; private set; }

    /// <summary>Sets the PostgreSQL connection string and updates the corresponding host settings.</summary>
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

    /// <summary>Gets the supplied data source or creates one from the current connection settings.</summary>
    /// <returns>The PostgreSQL data source used to open transport connections.</returns>
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

    /// <summary>Creates a PostgreSQL connection-context factory for the specified host configuration.</summary>
    /// <param name="hostConfiguration">The host configuration used by new connection contexts.</param>
    /// <returns>The PostgreSQL connection-context factory.</returns>
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
