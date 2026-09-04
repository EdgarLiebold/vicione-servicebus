using System;
using System.Collections.Generic;
using System.Data;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a configuration sql host settings implementation.
/// </summary>
public abstract class ConfigurationSqlHostSettings :
    SqlHostSettings
{
    readonly Lazy<Uri> _hostAddress;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    protected ConfigurationSqlHostSettings(Uri address)
        : this()
    {
        var hostAddress = new SqlHostAddress(address);

        Host = hostAddress.Host;
        if (address.Port != -1)
            Port = address.Port;

        if (!string.IsNullOrWhiteSpace(address.UserInfo))
        {
            var parts = address.UserInfo.Split(':');
            Username = UriDecode(parts[0]);

            if (parts.Length >= 2)
                Password = UriDecode(parts[1]);
        }

        VirtualHost = hostAddress.VirtualHost;
        Area = hostAddress.Area;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected ConfigurationSqlHostSettings()
    {
        VirtualHost = "/";

        IsolationLevel = IsolationLevel.RepeatableRead;

        ConnectionLimit = 10;

        _hostAddress = new Lazy<Uri>(FormatHostAddress);

        MaintenanceEnabled = true;
        MaintenanceInterval = TimeSpan.FromSeconds(5);
        QueueCleanupInterval = TimeSpan.FromMinutes(1);
        MaintenanceBatchSize = 10000;
    }

    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public string? Host { get; set; }
    /// <summary>
    /// Gets or sets the instance name value.
    /// </summary>
    public string? InstanceName { get; set; }
    /// <summary>
    /// Gets or sets the port value.
    /// </summary>
    public int? Port { get; set; }
    /// <summary>
    /// Gets or sets the database value.
    /// </summary>
    public string? Database { get; set; }
    /// <summary>
    /// Gets or sets the schema value.
    /// </summary>
    public string? Schema { get; set; }
    /// <summary>
    /// Gets or sets the username value.
    /// </summary>
    public string? Username { get; set; }
    /// <summary>
    /// Gets or sets the password value.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the isolation level value.
    /// </summary>
    public IsolationLevel IsolationLevel { get; set; }

    /// <summary>
    /// Gets or sets the connection limit value.
    /// </summary>
    public int ConnectionLimit { get; set; }

    /// <summary>
    /// Gets or sets the maintenance enabled value.
    /// </summary>
    public bool MaintenanceEnabled { get; set; }
    /// <summary>
    /// Gets or sets the maintenance interval value.
    /// </summary>
    public TimeSpan MaintenanceInterval { get; set; }
    /// <summary>
    /// Gets or sets the queue cleanup interval value.
    /// </summary>
    public TimeSpan QueueCleanupInterval { get; set; }
    /// <summary>
    /// Gets or sets the maintenance batch size value.
    /// </summary>
    public int MaintenanceBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the connection tag value.
    /// </summary>
    public string? ConnectionTag { get; set; }

    /// <summary>
    /// Gets or sets the virtual host value.
    /// </summary>
    public string? VirtualHost { get; set; }
    /// <summary>
    /// Gets or sets the area value.
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// Creates connection context factory.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public abstract ConnectionContextFactory CreateConnectionContextFactory(ISqlHostConfiguration configuration);

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress => _hostAddress.Value;

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            yield return this.Failure("Host", "Host must be specified");

        if (ConnectionLimit < 1)
            yield return this.Failure("ConnectionLimit", "must be >= 1");
    }

    static string UriDecode(string uri)
    {
        return Uri.UnescapeDataString(uri.Replace("+", "%2B"));
    }

    Uri FormatHostAddress()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new ConfigurationException("Host cannot be empty");
        if (string.IsNullOrWhiteSpace(VirtualHost))
            throw new ConfigurationException("Domain cannot be empty");

        return new SqlHostAddress(Host!, InstanceName, Port, VirtualHost!, Area);
    }
}
