using System;
using System.Data;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Configures sql host.</summary>
public abstract class SqlHostConfigurator :
    ISqlHostConfigurator
{
    readonly ConfigurationSqlHostSettings _settings;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    protected SqlHostConfigurator(ConfigurationSqlHostSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Gets or sets the connection string.</summary>
    public abstract string? ConnectionString { set; }

    /// <summary>Gets or sets the connection tag.</summary>
    public string? ConnectionTag
    {
        set => _settings.ConnectionTag = value;
    }

    /// <summary>Gets or sets the host.</summary>
    public string? Host
    {
        set => _settings.Host = value;
    }

    /// <summary>Gets or sets the instance name.</summary>
    public string? InstanceName
    {
        set => _settings.InstanceName = value;
    }

    /// <summary>Gets or sets the port.</summary>
    public int? Port
    {
        set => _settings.Port = value;
    }

    /// <summary>Gets or sets the database.</summary>
    public string? Database
    {
        set => _settings.Database = value;
    }

    /// <summary>Gets or sets the schema.</summary>
    public string? Schema
    {
        set => _settings.Schema = value;
    }

    /// <summary>Gets or sets the username.</summary>
    public string? Username
    {
        set => _settings.Username = value;
    }

    /// <summary>Gets or sets the password.</summary>
    public string? Password
    {
        set => _settings.Password = value;
    }

    /// <summary>Gets or sets the virtual host.</summary>
    public string? VirtualHost
    {
        set => _settings.VirtualHost = value;
    }

    /// <summary>Gets or sets the area.</summary>
    public string? Area
    {
        set => _settings.Area = value;
    }

    /// <summary>Gets or sets the isolation level.</summary>
    public IsolationLevel IsolationLevel
    {
        set => _settings.IsolationLevel = value;
    }

    /// <summary>Gets or sets the connection limit.</summary>
    public int ConnectionLimit
    {
        set => _settings.ConnectionLimit = value;
    }

    /// <summary>Gets or sets the maintenance enabled.</summary>
    public bool MaintenanceEnabled
    {
        set => _settings.MaintenanceEnabled = value;
    }

    /// <summary>Gets or sets the maintenance interval.</summary>
    public TimeSpan MaintenanceInterval
    {
        set => _settings.MaintenanceInterval = value;
    }

    /// <summary>Gets or sets the queue cleanup interval.</summary>
    public TimeSpan QueueCleanupInterval
    {
        set => _settings.QueueCleanupInterval = value;
    }

    /// <summary>Gets or sets the maintenance batch size.</summary>
    public int MaintenanceBatchSize
    {
        set => _settings.MaintenanceBatchSize = value;
    }
}
