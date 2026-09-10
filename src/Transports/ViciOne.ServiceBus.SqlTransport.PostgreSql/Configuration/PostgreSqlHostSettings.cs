using System;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Defines PostgreSQL connection and maintenance settings for a SQL transport host.</summary>
internal sealed class PostgreSqlHostSettings :
    ConfigurationSqlHostSettings
{
    readonly NpgsqlDataSource? _dataSource;
    NpgsqlConnectionStringBuilder? _builder;

    /// <summary>Initializes the settings from a PostgreSQL host address.</summary>
    /// <param name="hostAddress">The PostgreSQL host address.</param>
    public PostgreSqlHostSettings(Uri hostAddress)
        : base(hostAddress)
    {
    }

    /// <summary>Initializes the settings from a PostgreSQL connection string.</summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public PostgreSqlHostSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>Initializes the settings with a caller-owned PostgreSQL data source.</summary>
    /// <param name="dataSource">The preconfigured data source used to open connections.</param>
    public PostgreSqlHostSettings(NpgsqlDataSource dataSource)
    {
        if (dataSource == null)
            throw new ArgumentNullException(nameof(dataSource));

        _dataSource = dataSource;

        IsProvidedDataSource = true;

        ConnectionString = dataSource.ConnectionString;
    }

    /// <summary>Initializes the settings from SQL transport options.</summary>
    /// <param name="options">The SQL transport options.</param>
    public PostgreSqlHostSettings(SqlTransportOptions options)
    {
        var builder = PostgreSqlTransportConnection.CreateBuilder(options);

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

            MultipleHosts = null;
            Port = null;
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
        return new PostgreSqlConnectionContextFactory(hostConfiguration);
    }

    void ParseHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            Host = host;
            MultipleHosts = null;
            return;
        }

        string[] hostSegments = host.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hostSegments.Length == 0)
        {
            Host = null;
            MultipleHosts = null;
            return;
        }

        MultipleHosts = hostSegments.Length > 1 ? host.Trim() : null;
        string firstHost = hostSegments[0];

        if (firstHost[0] == '[')
        {
            int closingBracket = firstHost.IndexOf(']');
            if (closingBracket > 1)
            {
                Host = firstHost[1..closingBracket];

                if (hostSegments.Length == 1
                    && closingBracket + 1 < firstHost.Length
                    && firstHost[closingBracket + 1] == ':'
                    && int.TryParse(firstHost[(closingBracket + 2)..], out int port)
                    && port is > 0 and <= 65535
                    && port != NpgsqlConnection.DefaultPort)
                    Port = port;

                return;
            }
        }

        int firstColon = firstHost.IndexOf(':');
        int lastColon = firstHost.LastIndexOf(':');
        if (firstColon > 0
            && firstColon == lastColon
            && int.TryParse(firstHost[(lastColon + 1)..], out int singleHostPort)
            && singleHostPort is > 0 and <= 65535)
        {
            Host = firstHost[..lastColon];
            if (hostSegments.Length == 1 && singleHostPort != NpgsqlConnection.DefaultPort)
                Port = singleHostPort;
            return;
        }

        Host = firstHost;
    }
}
