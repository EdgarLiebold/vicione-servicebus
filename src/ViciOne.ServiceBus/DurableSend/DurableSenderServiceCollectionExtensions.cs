#nullable enable

namespace Microsoft.Extensions.DependencyInjection;

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.DurableSend;
using ViciOne.ServiceBus.Diagnostics;

/// <summary>DI registration for the generic producer-side durable sender.</summary>
public static class DurableSenderServiceCollectionExtensions
{
    /// <summary>
    /// Adds one durable sender runtime for <typeparamref name="TBus"/>. A matching <see cref="IDurableSendStore{TBus}"/>
    /// and <see cref="IDurableSendDispatcher{TBus}"/> must also be registered.
    /// </summary>
    public static IServiceCollection AddViciOneDurableSender<TBus>(
        this IServiceCollection services,
        Action<DurableSenderOptions<TBus>>? configure = null)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMetrics();

        var options = services.AddOptions<DurableSenderOptions<TBus>>();
        if (configure is not null)
            options.Configure(configure);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<V5ServiceBusInstrumentation<TBus>>();
        services.TryAddSingleton<DurableSenderPolicy<TBus>>(provider =>
            provider.GetRequiredService<IOptions<DurableSenderOptions<TBus>>>().Value.ValidateAndFreeze());
        services.TryAddSingleton<IDurableSender<TBus>, DurableSender<TBus>>();
        services.TryAddSingleton<IDurableSenderOperations<TBus>, DurableSenderOperations<TBus>>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DurableSenderDeliveryService<TBus>>());

        return services;
    }
}
