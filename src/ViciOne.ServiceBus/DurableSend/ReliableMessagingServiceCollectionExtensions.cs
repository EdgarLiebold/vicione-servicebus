using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Diagnostics;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;


namespace Microsoft.Extensions.DependencyInjection;
/// <summary>Registers the one reliable-messaging runtime owned by each bus.</summary>
public static class ReliableMessagingServiceCollectionExtensions
{
    /// <summary>Adds and configures reliable messaging within the default bus configuration flow.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The configured reliable messaging.</returns>
    public static IBusRegistrationConfigurator UseReliableMessaging(
        this IBusRegistrationConfigurator configurator,
        Action<IReliableMessagingConfigurator<IBus>> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ConfigureReliableMessaging(configurator, configure);
        return configurator;
    }

    /// <summary>Adds and configures reliable messaging within its owning typed bus configuration flow.</summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The configured reliable messaging.</returns>
    public static IBusRegistrationConfigurator<TBus> UseReliableMessaging<TBus>(
        this IBusRegistrationConfigurator<TBus> configurator,
        Action<IReliableMessagingConfigurator<TBus>> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ConfigureReliableMessaging(configurator, configure);
        return configurator;
    }

    /// <summary>
    /// Provider/testing-level registration for one durable sender runtime. Application configuration should use
    /// <see cref="UseReliableMessaging(IBusRegistrationConfigurator,Action{IReliableMessagingConfigurator{IBus}})"/>.
    /// </summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddViciOneReliableMessaging<TBus>(
        this IServiceCollection services,
        Action<ReliableMessagingOptions<TBus>>? configure = null)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMetrics();

        var options = services.AddOptions<ReliableMessagingOptions<TBus>>();
        if (configure is not null)
        {
            options.Configure(value =>
            {
                configure(value);
                value.StoreLimitsConfigured = true;
                value.DeliveryConfigured = true;
                value.RetentionConfigured = true;
                if (value.Retention == default)
                    value.Retention = TimeSpan.FromDays(7);
            });
        }
        options.ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<ReliableMessagingOptions<TBus>>, ReliableMessagingOptionsValidator<TBus>>());

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<V5ServiceBusInstrumentation<TBus>>();
        services.TryAddSingleton<ReliableMessagingPolicy<TBus>>(provider =>
            provider.GetRequiredService<IOptions<ReliableMessagingOptions<TBus>>>().Value.ValidateAndFreeze());
        services.TryAddSingleton<IDurableSendAdmission<TBus>, DurableSendAdmission<TBus>>();
        services.TryAddScoped<IDurableSender<TBus>>(provider => new TypedDurableSender<TBus>(
            provider.GetRequiredService<TBus>(),
            provider.GetRequiredService<IMessageContractCatalog>(),
            provider.GetRequiredService<IDurableSendAdmission<TBus>>(),
            provider.GetService<PayloadAdmissionRuntime<TBus>>()));
        ReliableSchedulerRegistration.AddStored<TBus>(services);
        services.TryAddSingleton<IReliableMessagingOperations<TBus>, ReliableMessagingOperations<TBus>>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ReliableMessagingDeliveryService<TBus>>());

        return services;
    }

    private static void ConfigureReliableMessaging<TBus>(
        IBusRegistrationConfigurator registrationConfigurator,
        Action<IReliableMessagingConfigurator<TBus>> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(registrationConfigurator);
        ArgumentNullException.ThrowIfNull(configure);
        IServiceCollection services = registrationConfigurator.Services;
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(ReliableMessagingRegistration<TBus>)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"Reliable messaging was already configured for bus '{typeof(TBus)}'.", "Configure it exactly once in the owning bus block"));
        }

        services.AddSingleton<ReliableMessagingRegistration<TBus>>();
        var schedulerSelection = new ReliableSchedulerSelection<TBus>();
        services.AddSingleton(schedulerSelection);
        BusCompositionRegistrations.AddFeature<TBus>(services, "Reliable messaging");
        services.AddViciOneReliableMessaging<TBus>();
        configure(new ReliableMessagingConfigurator<TBus>(registrationConfigurator, schedulerSelection));
    }

}

internal sealed class ReliableMessagingOptionsValidator<TBus> : IValidateOptions<ReliableMessagingOptions<TBus>>
    where TBus : class, IBus
{
    public ValidateOptionsResult Validate(string? name, ReliableMessagingOptions<TBus> options)
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
