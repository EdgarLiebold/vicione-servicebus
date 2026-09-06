namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines configuration options for sql transport.
/// </summary>
public sealed class SqlTransportOptions
{
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public string? Host { get; set; }
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
    /// Gets or sets the role value.
    /// </summary>
    public string? Role { get; set; }
    /// <summary>
    /// Gets or sets the username value.
    /// </summary>
    public string? Username { get; set; }
    /// <summary>
    /// Gets or sets the password value.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the admin username value.
    /// </summary>
    public string? AdminUsername { get; set; }
    /// <summary>
    /// Gets or sets the admin password value.
    /// </summary>
    public string? AdminPassword { get; set; }

    /// <summary>
    /// Optional, if specified, will be parsed to capture additional properties on the connection.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// If specified, changes the connection limit from the default value (10)
    /// </summary>
    public int? ConnectionLimit { get; set; }

    /// <summary>
    /// Disable maintenance and cleanup jobs (metrics consolidation, topology cleanup, etc.)
    /// Should typically be left to the default (false), reserved for use cases such as delegating maintenance activities explicitly as application quantities grow.
    /// </summary>
    public bool DisableMaintenance { get; set; }
}
