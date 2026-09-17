using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Courier;

static class CourierServiceRegistration
{
    public static void Register(IServiceCollection services, Type busType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(busType);

        if (!typeof(IBus).IsAssignableFrom(busType) || busType.ContainsGenericParameters)
            throw new ArgumentException($"The bus type must be closed and implement {TypeCache<IBus>.ShortName}.", nameof(busType));

        CourierCorrelationConventions.Register();

        services.TryAddScoped<IRoutingSlipExecutor>(provider =>
        {
            ScopedBusContext context = provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context;
            return new RoutingSlipExecutor(context.SendEndpointProvider, context.PublishEndpoint,
                provider.GetService<TimeProvider>() ?? TimeProvider.System);
        });

        if (busType == typeof(IBus))
            return;

        MethodInfo registerMethod = typeof(CourierServiceRegistration).GetMethod(nameof(RegisterBus),
                BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The {nameof(RegisterBus)} method was not found.");
        registerMethod.MakeGenericMethod(busType).Invoke(null, new object[] { services });
    }

    static void RegisterBus<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        services.TryAddScoped(provider =>
        {
            ScopedBusContext context = provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context;
            return Bind<TBus>.Create<IRoutingSlipExecutor>(new RoutingSlipExecutor(context.SendEndpointProvider,
                context.PublishEndpoint, provider.GetService<TimeProvider>() ?? TimeProvider.System));
        });
    }
}
