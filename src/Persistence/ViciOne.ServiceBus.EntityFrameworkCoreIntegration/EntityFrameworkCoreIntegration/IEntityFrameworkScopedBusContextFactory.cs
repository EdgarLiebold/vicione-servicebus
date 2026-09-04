using System;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

internal interface IEntityFrameworkScopedBusContextFactory<TBus>
    where TBus : class, IBus
{
    Type DbContextType { get; }
    bool IsDefault { get; }
    ScopedBusContext Create(IServiceProvider provider);
}
