using System;
using System.Data;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Applies fluent host configuration to provider-owned SQL transport settings.</summary>
public abstract class SqlHostConfigurator :
    ISqlHostConfigurator
{
    readonly ConfigurationSqlHostSettings _settings;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    protected SqlHostConfigurator(ConfigurationSqlHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
    }

    /// <summary>Sets the provider connection string.</summary>
    public abstract string? ConnectionString { set; }

    /// <summary>Sets the connection tag.</summary>
    public string? ConnectionTag
    {
        set => _settings.ConnectionTag = value;
    }

    /// <summary>Sets the database server host.</summary>
    public string? Host
    {
        set => _settings.Host = value;
    }

    /// <summary>Sets the SQL Server instance name.</summary>
    public string? InstanceName
    {
        set => _settings.InstanceName = value;
    }

    /// <summary>Sets the database server port.</summary>
    public int? Port
    {
        set => _settings.Port = value;
    }

    /// <summary>Sets the transport database name.</summary>
    public string? Database
    {
        set => _settings.Database = value;
    }

    /// <summary>Sets the transport schema.</summary>
    public string? Schema
    {
        set => _settings.Schema = value;
    }

    /// <summary>Sets the database login name.</summary>
    public string? Username
    {
        set => _settings.Username = value;
    }

    /// <summary>Sets the database login password.</summary>
    public string? Password
    {
        set => _settings.Password = value;
    }

    /// <summary>Sets the logical transport namespace.</summary>
    public string? VirtualHost
    {
        set => _settings.VirtualHost = value;
    }

    /// <summary>Sets the logical queue area.</summary>
    public string? Area
    {
        set => _settings.Area = value;
    }

    /// <summary>Sets the transaction isolation level.</summary>
    public IsolationLevel IsolationLevel
    {
        set => _settings.IsolationLevel = value;
    }

    /// <summary>Sets the connection limit.</summary>
    public int ConnectionLimit
    {
        set => _settings.ConnectionLimit = value;
    }

    /// <summary>Sets whether automatic maintenance is enabled.</summary>
    public bool MaintenanceEnabled
    {
        set => _settings.MaintenanceEnabled = value;
    }

    /// <summary>Sets the maintenance interval.</summary>
    public TimeSpan MaintenanceInterval
    {
        set => _settings.MaintenanceInterval = value;
    }

    /// <summary>Sets the queue cleanup interval.</summary>
    public TimeSpan QueueCleanupInterval
    {
        set => _settings.QueueCleanupInterval = value;
    }

    /// <summary>Sets the maintenance batch size.</summary>
    public int MaintenanceBatchSize
    {
        set => _settings.MaintenanceBatchSize = value;
    }
}
