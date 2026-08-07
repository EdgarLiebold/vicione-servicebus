// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;
using EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


public interface IDatabaseTestConfiguration
{
    ILockStatementProvider LockStatementProvider { get; }

    IServiceCollection Create();

    void Configure(IBusRegistrationConfigurator configurator, Action<IBusRegistrationContext, ISqlBusFactoryConfigurator> callback);

    void Apply<TDbContext>(DbContextOptionsBuilder builder)
        where TDbContext : DbContext;
}
