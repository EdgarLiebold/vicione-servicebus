using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>
/// Provides a sql server sql host configurator implementation.
/// </summary>
public class SqlServerSqlHostConfigurator :
    SqlHostConfigurator,
    ISqlServerSqlHostConfigurator
{
    readonly SqlServerSqlHostSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public SqlServerSqlHostConfigurator(SqlServerSqlHostSettings settings)
        : base(settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    public SqlServerSqlHostConfigurator(Uri hostAddress)
        : this(new SqlServerSqlHostSettings(hostAddress))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public SqlServerSqlHostConfigurator(SqlTransportOptions options)
        : this(new SqlServerSqlHostSettings(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionString">The connection string value.</param>
    public SqlServerSqlHostConfigurator(string connectionString)
        : this(new SqlServerSqlHostSettings(connectionString))
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
