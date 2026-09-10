namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Selects the SQL transport provisioning operations performed by the migration hosted service.</summary>
public sealed class SqlTransportMigrationOptions
{
    /// <summary>Gets or sets whether the transport database is created during startup.</summary>
    public bool CreateDatabase { get; set; }

    /// <summary>
    /// Gets or sets whether the transport schema and its access role are created or updated during startup.
    /// </summary>
    public bool CreateSchema { get; set; }

    /// <summary>
    /// Gets or sets whether transport tables, indexes, routines, and views are created or updated during startup.
    /// </summary>
    public bool CreateInfrastructure { get; set; }

    /// <summary>Gets or sets whether the transport database is deleted during application shutdown.</summary>
    public bool DeleteDatabase { get; set; }
}
