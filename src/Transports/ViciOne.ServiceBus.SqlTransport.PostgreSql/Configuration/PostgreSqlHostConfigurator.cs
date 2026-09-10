using System;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Configures PostgreSQL-specific settings for a SQL transport host.</summary>
internal sealed class PostgreSqlHostConfigurator :
    SqlHostConfigurator
{
    readonly PostgreSqlHostSettings _settings;

    /// <summary>Initializes the configurator with existing PostgreSQL host settings.</summary>
    /// <param name="settings">The settings to configure.</param>
    public PostgreSqlHostConfigurator(PostgreSqlHostSettings settings)
        : base(settings)
    {
        _settings = settings;
    }

    /// <summary>Initializes the configurator from a PostgreSQL host address.</summary>
    /// <param name="hostAddress">The PostgreSQL host address.</param>
    public PostgreSqlHostConfigurator(Uri hostAddress)
        : this(new PostgreSqlHostSettings(hostAddress))
    {
    }

    /// <summary>Initializes the configurator from SQL transport options.</summary>
    /// <param name="options">The SQL transport options.</param>
    public PostgreSqlHostConfigurator(SqlTransportOptions options)
        : this(new PostgreSqlHostSettings(options))
    {
    }

    /// <summary>Initializes the configurator from a PostgreSQL connection string.</summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public PostgreSqlHostConfigurator(string connectionString)
        : this(new PostgreSqlHostSettings(connectionString))
    {
    }

    /// <summary>Initializes the configurator with a preconfigured PostgreSQL data source.</summary>
    /// <param name="dataSource">The data source used to open connections.</param>
    public PostgreSqlHostConfigurator(NpgsqlDataSource dataSource)
        : this(new PostgreSqlHostSettings(dataSource))
    {
    }

    /// <summary>Gets the configured PostgreSQL host settings.</summary>
    public SqlHostSettings Settings => _settings;

    /// <summary>Sets the PostgreSQL connection string represented by the host settings.</summary>
    public override string? ConnectionString
    {
        set => _settings.ConnectionString = value;
    }
}
