using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Configures SQL Server-specific settings for a SQL transport host.</summary>
public class SqlServerSqlHostConfigurator :
    SqlHostConfigurator,
    ISqlServerSqlHostConfigurator
{
    readonly SqlServerSqlHostSettings _settings;

    /// <summary>Initializes the configurator with existing SQL Server host settings.</summary>
    /// <param name="settings">The settings to configure.</param>
    public SqlServerSqlHostConfigurator(SqlServerSqlHostSettings settings)
        : base(settings)
    {
        _settings = settings;
    }

    /// <summary>Initializes the configurator from a SQL Server host address.</summary>
    /// <param name="hostAddress">The SQL Server host address.</param>
    public SqlServerSqlHostConfigurator(Uri hostAddress)
        : this(new SqlServerSqlHostSettings(hostAddress))
    {
    }

    /// <summary>Initializes the configurator from SQL transport options.</summary>
    /// <param name="options">The SQL transport options.</param>
    public SqlServerSqlHostConfigurator(SqlTransportOptions options)
        : this(new SqlServerSqlHostSettings(options))
    {
    }

    /// <summary>Initializes the configurator from a SQL Server connection string.</summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public SqlServerSqlHostConfigurator(string connectionString)
        : this(new SqlServerSqlHostSettings(connectionString))
    {
    }

    /// <summary>Gets the configured SQL Server host settings.</summary>
    public SqlHostSettings Settings => _settings;

    /// <summary>Sets the SQL Server connection string represented by the host settings.</summary>
    public override string? ConnectionString
    {
        set => _settings.ConnectionString = value;
    }
}
