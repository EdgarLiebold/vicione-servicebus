using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

public class SqlServerConnectionContextFactory :
    ConnectionContextFactory
{
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly SqlServerSqlHostSettings _hostSettings;

    public SqlServerConnectionContextFactory(ISqlHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _hostSettings = hostConfiguration.Settings as SqlServerSqlHostSettings
            ?? throw new ConfigurationException("The host settings were not of the expected type");
    }

    protected override ConnectionContext CreateConnection(ITransportSupervisor<ConnectionContext> supervisor)
    {
        return new SqlServerDbConnectionContext(_hostConfiguration, supervisor);
    }
}
