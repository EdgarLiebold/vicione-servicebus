namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;
using System.Reflection;
using EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


public class PostgresDatabaseTestConfiguration :
    IDatabaseTestConfiguration
{
    /// <summary>
    /// The run-scoped fixture the runner published. See <see cref="RunScopedTransportEndpoint"/> for
    /// why the literal endpoint that stood here could not be measured reliably.
    /// </summary>
    static string ConnectionString => RunScopedTransportEndpoint.PostgresConnectionString("ViciOneServiceBusUnitTests");

    public IServiceCollection Create()
    {
        return new ServiceCollection()
            .ConfigurePostgresTransport();
    }

    public ILockStatementProvider LockStatementProvider => new PostgresLockStatementProvider(false);

    public void Apply<TDbContext>(DbContextOptionsBuilder builder)
        where TDbContext : DbContext
    {
        builder.UseNpgsql(ConnectionString, m =>
        {
            m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
            m.MigrationsHistoryTable($"__{typeof(TDbContext).Name}");
        });
    }

    public void Configure(IBusRegistrationConfigurator configurator, Action<IBusRegistrationContext, ISqlBusFactoryConfigurator> callback)
    {
        configurator.ConfigurePostgresTransport();

        configurator.AddConfigureEndpointsCallback((_, cfg) =>
        {
            if (cfg is ISqlReceiveEndpointConfigurator db)
                db.PurgeOnStartup = true;
        });

        configurator.UsingPostgres((context, cfg) =>
        {
            callback(context, cfg);
        });
    }
}
