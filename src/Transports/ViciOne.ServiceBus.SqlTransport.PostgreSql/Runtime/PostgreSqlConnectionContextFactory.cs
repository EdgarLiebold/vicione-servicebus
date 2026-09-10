using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Creates supervised PostgreSQL connection contexts for a configured SQL host.</summary>
internal sealed class PostgreSqlConnectionContextFactory :
    ConnectionContextFactory
{
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly PostgreSqlHostSettings _hostSettings;

    /// <summary>Initializes the factory from a PostgreSQL host configuration.</summary>
    /// <param name="hostConfiguration">The host configuration whose settings are used by new contexts.</param>
    public PostgreSqlConnectionContextFactory(ISqlHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        _hostSettings = hostConfiguration.Settings as PostgreSqlHostSettings
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("SQL transport", "unknown", "The host settings were not of the expected type", "Correct the named configuration before starting the host"));
    }

    /// <summary>Creates a PostgreSQL connection context supervised by the specified transport supervisor.</summary>
    /// <param name="supervisor">The supervisor that controls the connection context.</param>
    /// <returns>The new PostgreSQL connection context.</returns>
    protected override ConnectionContext CreateConnection(ITransportSupervisor<ConnectionContext> supervisor)
    {
        return new PostgreSqlDbConnectionContext(_hostConfiguration, supervisor);
    }
}
