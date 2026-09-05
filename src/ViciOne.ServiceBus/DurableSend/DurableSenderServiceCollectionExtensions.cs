using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Diagnostics;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;

#nullable enable

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>DI registration for the generic producer-side durable sender.</summary>
public static class DurableSenderServiceCollectionExtensions
{
    /// <summary>Adds and configures Durable Sender within the default bus configuration flow.</summary>
    public static IBusRegistrationConfigurator UseDurableSender(
        this IBusRegistrationConfigurator configurator,
        Action<IDurableSenderConfigurator<IBus>> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ConfigureDurableSender(configurator.Services, configure);
        return configurator;
    }

    /// <summary>Adds and configures Durable Sender within its owning typed bus configuration flow.</summary>
    public static IBusRegistrationConfigurator<TBus> UseDurableSender<TBus>(
        this IBusRegistrationConfigurator<TBus> configurator,
        Action<IDurableSenderConfigurator<TBus>> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ConfigureDurableSender(configurator.Services, configure);
        return configurator;
    }

    /// <summary>
    /// Provider/testing-level registration for one durable sender runtime. Application configuration should use
    /// <see cref="UseDurableSender(IBusRegistrationConfigurator,Action{IDurableSenderConfigurator{IBus}})"/>.
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
        options.ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<DurableSenderOptions<TBus>>, DurableSenderOptionsValidator<TBus>>());

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<V5ServiceBusInstrumentation<TBus>>();
        services.TryAddSingleton<DurableSenderPolicy<TBus>>(provider =>
            provider.GetRequiredService<IOptions<DurableSenderOptions<TBus>>>().Value.ValidateAndFreeze());
        services.TryAddSingleton<IDurableSendAdmission<TBus>, DurableSendAdmission<TBus>>();
        services.TryAddScoped<IDurableSender<TBus>>(provider => new TypedDurableSender<TBus>(
            provider.GetRequiredService<TBus>(),
            provider.GetRequiredService<IMessageContractCatalog>(),
            provider.GetRequiredService<IDurableSendAdmission<TBus>>(),
            provider.GetService<PayloadAdmissionRuntime<TBus>>()));
        services.TryAddSingleton<IDurableSenderOperations<TBus>, DurableSenderOperations<TBus>>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DurableSenderDeliveryService<TBus>>());

        return services;
    }

    private static void ConfigureDurableSender<TBus>(
        IServiceCollection services,
        Action<IDurableSenderConfigurator<TBus>> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(DurableSenderRegistration<TBus>)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"Durable Sender was already configured for bus '{typeof(TBus)}'. Configure it exactly once in the owning bus block.", "Correct the named configuration before starting the host"));
        }

        services.AddSingleton<DurableSenderRegistration<TBus>>();
        BusCompositionRegistrations.AddFeature<TBus>(services, "Reliable messaging");
        services.AddViciOneDurableSender<TBus>();
        configure(new DurableSenderConfigurator<TBus>(services));
    }

}

internal sealed class DurableSenderOptionsValidator<TBus> : IValidateOptions<DurableSenderOptions<TBus>>
    where TBus : class, IBus
{
    public ValidateOptionsResult Validate(string? name, DurableSenderOptions<TBus> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            _ = options.ValidateAndFreeze();
            return ValidateOptionsResult.Success;
        }
        catch (ConfigurationException exception)
        {
            string bus = ViciOne.ServiceBus.Configuration.BusRegistrationIdentity.GetKey(typeof(TBus));
            return ValidateOptionsResult.Fail(
                $"Reliable messaging for bus '{bus}': {exception.Message} Correct the named value before starting the host.");
        }
    }
}
