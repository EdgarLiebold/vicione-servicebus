using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Projects SQL Server connection values onto a SQL transport host.</summary>
internal sealed class SqlServerHostConfigurator :
    SqlHostConfigurator
{
    readonly SqlServerHostSettings _settings;

    /// <summary>Initializes the configurator with existing SQL Server host settings.</summary>
    /// <param name="settings">The settings to configure.</param>
    public SqlServerHostConfigurator(SqlServerHostSettings settings)
        : base(settings)
    {
        _settings = settings;
    }

    /// <summary>Initializes the configurator from a SQL Server host address.</summary>
    /// <param name="hostAddress">The SQL Server host address.</param>
    public SqlServerHostConfigurator(Uri hostAddress)
        : this(new SqlServerHostSettings(hostAddress))
    {
    }

    /// <summary>Initializes the configurator from SQL transport options.</summary>
    /// <param name="options">The SQL transport options.</param>
    public SqlServerHostConfigurator(SqlTransportOptions options)
        : this(new SqlServerHostSettings(options))
    {
    }

    /// <summary>Initializes the configurator from a SQL Server connection string.</summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public SqlServerHostConfigurator(string connectionString)
        : this(new SqlServerHostSettings(connectionString))
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
