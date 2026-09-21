using System;
using System.Collections.Generic;
using System.Data;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Stores the provider-neutral connection and maintenance settings used to build a SQL transport host.</summary>
public abstract class ConfigurationSqlHostSettings :
    SqlHostSettings
{
    /// <summary>Initializes the settings from an absolute SQL transport host address.</summary>
    /// <param name="address">The host address to project onto these settings.</param>
    protected ConfigurationSqlHostSettings(Uri address)
        : this()
    {
        var hostAddress = new SqlHostAddress(address);

        Host = hostAddress.Host;
        if (address.Port != -1)
            Port = address.Port;

        if (!string.IsNullOrWhiteSpace(address.UserInfo))
        {
            int separator = address.UserInfo.IndexOf(':');
            Username = UriDecode(separator < 0 ? address.UserInfo : address.UserInfo[..separator]);

            if (separator >= 0)
                Password = UriDecode(address.UserInfo[(separator + 1)..]);
        }

        VirtualHost = hostAddress.VirtualHost;
        Area = hostAddress.Area;
    }

    /// <summary>Initializes the settings with transport defaults.</summary>
    protected ConfigurationSqlHostSettings()
    {
        VirtualHost = "/";

        IsolationLevel = IsolationLevel.RepeatableRead;

        ConnectionLimit = 10;

        MaintenanceEnabled = true;
        MaintenanceInterval = TimeSpan.FromSeconds(5);
        QueueCleanupInterval = TimeSpan.FromMinutes(1);
        MaintenanceBatchSize = 10000;
    }

    /// <summary>Gets or sets the host.</summary>
    public virtual string? Host { get; set; }
    /// <summary>Gets or sets the instance name.</summary>
    public string? InstanceName { get; set; }
    /// <summary>Gets or sets the port.</summary>
    public int? Port { get; set; }
    /// <summary>Gets or sets the database.</summary>
    public string? Database { get; set; }
    /// <summary>Gets or sets the schema.</summary>
    public string? Schema { get; set; }
    /// <summary>Gets or sets the username.</summary>
    public string? Username { get; set; }
    /// <summary>Gets or sets the password.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the isolation level.</summary>
    public IsolationLevel IsolationLevel { get; set; }

    /// <summary>Gets or sets the connection limit.</summary>
    public int ConnectionLimit { get; set; }

    /// <summary>Gets or sets the maintenance enabled.</summary>
    public bool MaintenanceEnabled { get; set; }
    /// <summary>Gets or sets the maintenance interval.</summary>
    public TimeSpan MaintenanceInterval { get; set; }
    /// <summary>Gets or sets the queue cleanup interval.</summary>
    public TimeSpan QueueCleanupInterval { get; set; }
    /// <summary>Gets or sets the maintenance batch size.</summary>
    public int MaintenanceBatchSize { get; set; }

    /// <summary>Gets or sets the connection tag.</summary>
    public string? ConnectionTag { get; set; }

    /// <summary>Gets or sets the virtual host.</summary>
    public string? VirtualHost { get; set; }
    /// <summary>Gets or sets the area.</summary>
    public string? Area { get; set; }

    /// <summary>Creates the provider connection factory for a validated host configuration.</summary>
    /// <param name="configuration">The host configuration that owns the connection lifecycle.</param>
    /// <returns>The provider connection-context factory.</returns>
    public abstract ConnectionContextFactory CreateConnectionContextFactory(ISqlHostConfiguration configuration);

    /// <summary>Gets an address that reflects the current host settings.</summary>
    public Uri HostAddress => FormatHostAddress();

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        foreach (ValidationResult result in ValidateAddress())
            yield return result;

        foreach (ValidationResult result in ValidateOperationalSettings())
            yield return result;
    }

    IEnumerable<ValidationResult> ValidateAddress()
    {
        if (string.IsNullOrWhiteSpace(Host))
            yield return this.Failure("Host", "Host must be specified");
        else if (!SqlHostAddress.CanRepresentNetworkHost(Host))
            yield return this.Failure("Host", "must identify a network host; Unix socket paths are not supported by SQL transport addresses");

        if (InstanceName != null && string.IsNullOrWhiteSpace(InstanceName))
            yield return this.Failure("InstanceName", "must not be empty or whitespace when specified");

        if (VirtualHost != "/" && !SqlHostAddress.IsValidSymbol(VirtualHost))
            yield return this.Failure("VirtualHost", "must start with a letter or underscore and contain only letters, digits, or underscores");

        if (Area != null)
        {
            if (VirtualHost == "/")
                yield return this.Failure("Area", "requires a named virtual host");
            else if (!SqlHostAddress.IsValidSymbol(Area))
                yield return this.Failure("Area", "must start with a letter or underscore and contain only letters, digits, or underscores");
        }
    }

    IEnumerable<ValidationResult> ValidateOperationalSettings()
    {
        if (ConnectionLimit < 1)
            yield return this.Failure("ConnectionLimit", "must be >= 1");

        if (Port is <= 0 or > 65535)
            yield return this.Failure("Port", "must be between 1 and 65535 when specified");

        if (MaintenanceInterval <= TimeSpan.Zero)
            yield return this.Failure("MaintenanceInterval", "must be greater than zero");

        if (QueueCleanupInterval <= TimeSpan.Zero)
            yield return this.Failure("QueueCleanupInterval", "must be greater than zero");

        if (MaintenanceBatchSize < 1)
            yield return this.Failure("MaintenanceBatchSize", "must be >= 1");
    }

    static string UriDecode(string uri)
    {
        return Uri.UnescapeDataString(uri.Replace("+", "%2B"));
    }

    Uri FormatHostAddress()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("SQL transport", "unknown", "Host cannot be empty", "Correct the named configuration before starting the host"));
        if (string.IsNullOrWhiteSpace(VirtualHost))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("SQL transport", "unknown", "Domain cannot be empty", "Correct the named configuration before starting the host"));

        return new SqlHostAddress(Host!, InstanceName, Port, VirtualHost!, Area);
    }
}
