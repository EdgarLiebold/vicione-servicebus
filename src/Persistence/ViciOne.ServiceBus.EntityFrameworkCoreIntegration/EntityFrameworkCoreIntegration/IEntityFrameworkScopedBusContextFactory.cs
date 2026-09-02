namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using System;
using DependencyInjection;


internal interface IEntityFrameworkScopedBusContextFactory<TBus>
    where TBus : class, IBus
{
    Type DbContextType { get; }
    bool IsDefault { get; }
    ScopedBusContext Create(IServiceProvider provider);
}
