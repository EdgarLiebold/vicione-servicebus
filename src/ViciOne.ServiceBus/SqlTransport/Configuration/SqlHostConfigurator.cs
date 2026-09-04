using System;
using System.Data;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql host configurator implementation.
/// </summary>
public abstract class SqlHostConfigurator :
    ISqlHostConfigurator
{
    readonly ConfigurationSqlHostSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    protected SqlHostConfigurator(ConfigurationSqlHostSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Gets or sets the connection string value.
    /// </summary>
    public abstract string? ConnectionString { set; }

    /// <summary>
    /// Gets or sets the connection tag value.
    /// </summary>
    public string? ConnectionTag
    {
        set => _settings.ConnectionTag = value;
    }

    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public string? Host
    {
        set => _settings.Host = value;
    }

    /// <summary>
    /// Gets or sets the instance name value.
    /// </summary>
    public string? InstanceName
    {
        set => _settings.InstanceName = value;
    }

    /// <summary>
    /// Gets or sets the port value.
    /// </summary>
    public int? Port
    {
        set => _settings.Port = value;
    }

    /// <summary>
    /// Gets or sets the database value.
    /// </summary>
    public string? Database
    {
        set => _settings.Database = value;
    }

    /// <summary>
    /// Gets or sets the schema value.
    /// </summary>
    public string? Schema
    {
        set => _settings.Schema = value;
    }

    /// <summary>
    /// Gets or sets the username value.
    /// </summary>
    public string? Username
    {
        set => _settings.Username = value;
    }

    /// <summary>
    /// Gets or sets the password value.
    /// </summary>
    public string? Password
    {
        set => _settings.Password = value;
    }

    /// <summary>
    /// Gets or sets the virtual host value.
    /// </summary>
    public string? VirtualHost
    {
        set => _settings.VirtualHost = value;
    }

    /// <summary>
    /// Gets or sets the area value.
    /// </summary>
    public string? Area
    {
        set => _settings.Area = value;
    }

    /// <summary>
    /// Gets or sets the isolation level value.
    /// </summary>
    public IsolationLevel IsolationLevel
    {
        set => _settings.IsolationLevel = value;
    }

    /// <summary>
    /// Gets or sets the connection limit value.
    /// </summary>
    public int ConnectionLimit
    {
        set => _settings.ConnectionLimit = value;
    }

    /// <summary>
    /// Gets or sets the maintenance enabled value.
    /// </summary>
    public bool MaintenanceEnabled
    {
        set => _settings.MaintenanceEnabled = value;
    }

    /// <summary>
    /// Gets or sets the maintenance interval value.
    /// </summary>
    public TimeSpan MaintenanceInterval
    {
        set => _settings.MaintenanceInterval = value;
    }

    /// <summary>
    /// Gets or sets the queue cleanup interval value.
    /// </summary>
    public TimeSpan QueueCleanupInterval
    {
        set => _settings.QueueCleanupInterval = value;
    }

    /// <summary>
    /// Gets or sets the maintenance batch size value.
    /// </summary>
    public int MaintenanceBatchSize
    {
        set => _settings.MaintenanceBatchSize = value;
    }
}
