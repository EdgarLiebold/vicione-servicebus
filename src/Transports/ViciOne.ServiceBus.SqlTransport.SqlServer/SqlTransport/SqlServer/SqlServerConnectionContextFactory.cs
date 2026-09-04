using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>
/// Provides a sql server connection context factory implementation.
/// </summary>
public class SqlServerConnectionContextFactory :
    ConnectionContextFactory
{
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly SqlServerSqlHostSettings _hostSettings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    public SqlServerConnectionContextFactory(ISqlHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _hostSettings = hostConfiguration.Settings as SqlServerSqlHostSettings
            ?? throw new ConfigurationException("The host settings were not of the expected type");
    }

    /// <summary>
    /// Creates connection.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <returns>The result of the operation.</returns>
    protected override ConnectionContext CreateConnection(ITransportSupervisor<ConnectionContext> supervisor)
    {
        return new SqlServerDbConnectionContext(_hostConfiguration, supervisor);
    }
}
