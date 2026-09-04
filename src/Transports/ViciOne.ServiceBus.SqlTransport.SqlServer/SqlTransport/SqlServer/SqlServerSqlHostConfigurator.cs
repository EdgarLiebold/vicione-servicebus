using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

public class SqlServerSqlHostConfigurator :
    SqlHostConfigurator,
    ISqlServerSqlHostConfigurator
{
    readonly SqlServerSqlHostSettings _settings;

    public SqlServerSqlHostConfigurator(SqlServerSqlHostSettings settings)
        : base(settings)
    {
        _settings = settings;
    }

    public SqlServerSqlHostConfigurator(Uri hostAddress)
        : this(new SqlServerSqlHostSettings(hostAddress))
    {
    }

    public SqlServerSqlHostConfigurator(SqlTransportOptions options)
        : this(new SqlServerSqlHostSettings(options))
    {
    }

    public SqlServerSqlHostConfigurator(string connectionString)
        : this(new SqlServerSqlHostSettings(connectionString))
    {
    }

    public SqlHostSettings Settings => _settings;

    public override string? ConnectionString
    {
        set => _settings.ConnectionString = value;
    }
}
