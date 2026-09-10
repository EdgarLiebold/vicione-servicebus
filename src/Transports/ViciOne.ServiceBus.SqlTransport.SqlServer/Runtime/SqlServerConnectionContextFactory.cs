using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Creates supervised SQL Server connection contexts for a configured SQL host.</summary>
internal sealed class SqlServerConnectionContextFactory :
    ConnectionContextFactory
{
    readonly ISqlHostConfiguration _hostConfiguration;
    /// <summary>Initializes the factory from a SQL Server host configuration.</summary>
    /// <param name="hostConfiguration">The host configuration whose settings are used by new contexts.</param>
    public SqlServerConnectionContextFactory(ISqlHostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        _hostConfiguration = hostConfiguration;
        _ = hostConfiguration.Settings as SqlServerHostSettings
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("SQL transport", "unknown", "The host settings were not of the expected type", "Correct the named configuration before starting the host"));
    }

    /// <summary>Creates a SQL Server connection context supervised by the specified transport supervisor.</summary>
    /// <param name="supervisor">The supervisor that controls the connection context.</param>
    /// <returns>The new SQL Server connection context.</returns>
    protected override ConnectionContext CreateConnection(ITransportSupervisor<ConnectionContext> supervisor)
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        return new SqlServerConnectionContext(_hostConfiguration, supervisor);
    }
}
