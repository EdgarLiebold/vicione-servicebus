using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

public class PostgresConnectionContextFactory :
    ConnectionContextFactory
{
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly PostgresSqlHostSettings _hostSettings;

    public PostgresConnectionContextFactory(ISqlHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _hostSettings = hostConfiguration.Settings as PostgresSqlHostSettings
            ?? throw new ConfigurationException("The host settings were not of the expected type");
    }

    protected override ConnectionContext CreateConnection(ITransportSupervisor<ConnectionContext> supervisor)
    {
        return new PostgresDbConnectionContext(_hostConfiguration, supervisor);
    }
}
