using System;
using System.Linq;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

internal static class SagaQueryCustomization
{
    public static IQueryable<TSaga> Apply<TSaga>(IQueryable<TSaga> query,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? customization)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(query);

        if (customization is null)
            return query;

        return customization(query)
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "The saga query customization returned null.", "Correct the named configuration before starting the host"));
    }
}
