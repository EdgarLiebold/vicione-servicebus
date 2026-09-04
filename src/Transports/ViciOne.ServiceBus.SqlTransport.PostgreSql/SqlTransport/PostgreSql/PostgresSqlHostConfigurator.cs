using System;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>
/// Provides a postgres sql host configurator implementation.
/// </summary>
public class PostgresSqlHostConfigurator :
    SqlHostConfigurator,
    IPostgresSqlHostConfigurator
{
    readonly PostgresSqlHostSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public PostgresSqlHostConfigurator(PostgresSqlHostSettings settings)
        : base(settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    public PostgresSqlHostConfigurator(Uri hostAddress)
        : this(new PostgresSqlHostSettings(hostAddress))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public PostgresSqlHostConfigurator(SqlTransportOptions options)
        : this(new PostgresSqlHostSettings(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionString">The connection string value.</param>
    public PostgresSqlHostConfigurator(string connectionString)
        : this(new PostgresSqlHostSettings(connectionString))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="dataSource">The data source value.</param>
    public PostgresSqlHostConfigurator(NpgsqlDataSource dataSource)
        : this(new PostgresSqlHostSettings(dataSource))
    {
    }

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public SqlHostSettings Settings => _settings;

    /// <summary>
    /// Gets or sets the connection string value.
    /// </summary>
    public override string? ConnectionString
    {
        set => _settings.ConnectionString = value;
    }
}
