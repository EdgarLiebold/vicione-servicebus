// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;
using System.Reflection;
using EntityFrameworkCoreIntegration;
using ViciOne.ServiceBus.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SqlServer;


public class SqlServerDatabaseTestConfiguration :
    IDatabaseTestConfiguration
{
    public ILockStatementProvider LockStatementProvider => new SqlServerLockStatementProvider(false);

    public IServiceCollection Create()
    {
        return new ServiceCollection()
            .ConfigureSqlServerTransport();
    }

    public void Apply<TDbContext>(DbContextOptionsBuilder builder)
        where TDbContext : DbContext
    {
        builder.UseSqlServer(LocalDbConnectionStringProvider.GetLocalDbConnectionString(), m =>
        {
            m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
            m.MigrationsHistoryTable($"__{typeof(TDbContext).Name}");
        });
    }

    public void Configure(IBusRegistrationConfigurator configurator, Action<IBusRegistrationContext, ISqlBusFactoryConfigurator> callback)
    {
        configurator.ConfigureSqlServerTransport();

        configurator.AddConfigureEndpointsCallback((_, cfg) =>
        {
            if (cfg is ISqlReceiveEndpointConfigurator db)
                db.PurgeOnStartup = true;
        });

        configurator.UsingSqlServer((context, cfg) =>
        {
            callback(context, cfg);
        });
    }
}
