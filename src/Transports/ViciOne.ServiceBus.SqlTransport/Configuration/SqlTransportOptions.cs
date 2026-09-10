namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures a provider-backed SQL transport connection and its maintenance behavior.</summary>
public sealed class SqlTransportOptions
{
    /// <summary>Gets or sets the database server host name.</summary>
    public string? Host { get; set; }
    /// <summary>Gets or sets the optional TCP port.</summary>
    public int? Port { get; set; }
    /// <summary>Gets or sets the transport database name.</summary>
    public string? Database { get; set; }
    /// <summary>Gets or sets the schema that contains transport objects.</summary>
    public string? Schema { get; set; } = "transport";
    /// <summary>Gets or sets the database role granted access to transport objects.</summary>
    public string? Role { get; set; } = "transport";
    /// <summary>Gets or sets the transport login name.</summary>
    public string? Username { get; set; }
    /// <summary>Gets or sets the transport login password.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the optional administrator login used only for provisioning.</summary>
    public string? AdminUsername { get; set; }
    /// <summary>Gets or sets the optional administrator password used only for provisioning.</summary>
    public string? AdminPassword { get; set; }

    /// <summary>Gets or sets a provider connection string. Explicit properties override corresponding connection-string values.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the maximum number of concurrent database operations, or <see langword="null" /> to use the transport default.</summary>
    public int? ConnectionLimit { get; set; }

    /// <summary>Gets or sets whether automatic metric consolidation and topology cleanup are disabled.</summary>
    public bool DisableMaintenance { get; set; }
}
