using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for service bus configuration.
/// </summary>
public static class ServiceBusConfigurationExtensions
{
    /// <summary>
    /// Configure and create a bus for Azure Service Bus
    /// </summary>
    /// <param name="selector">Hang off the selector interface for visibility</param>
    /// <param name="configure">The configuration callback to configure the bus</param>
    /// <returns></returns>
    public static IBusControl CreateUsingAzureServiceBus(this IBusFactorySelector selector, Action<IServiceBusBusFactoryConfigurator> configure)
    {
        return AzureBusFactory.CreateUsingServiceBus(configure);
    }

    /// <summary>
    /// Configure ViciOne.ServiceBus to use Azure Service Bus for the transport.
    /// </summary>
    /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
    /// <param name="configure">The configuration callback for the bus factory</param>
    public static void UsingAzureServiceBus(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>? configure = null)
    {
        AddTransportOptions(configurator.Services, string.Empty, "default");
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, ServiceBusSendFailureClassifier>());
        configurator.SetBusFactory(new ServiceBusRegistrationBusFactory(configure));

        configurator.Services.TryAddSingleton(provider =>
        {
            var subscriptionEndpointConnector = provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value as ISubscriptionEndpointConnector;

            return subscriptionEndpointConnector ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", "The default bus instance is not an Azure Service Bus Instance", "Correct the named configuration before starting the host"));
        });
    }

    /// <summary>
    /// Configure ViciOne.ServiceBus to use Azure Service Bus for the transport.
    /// </summary>
    /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
    /// <param name="configure">The configuration callback for the bus factory</param>
    public static void UsingAzureServiceBus<TBus>(this IBusRegistrationConfigurator<TBus> configurator,
        Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>? configure = null)
        where TBus : class, IBus
    {
        AddTransportOptions(configurator.Services, typeof(TBus).Name, typeof(TBus).FullName ?? typeof(TBus).Name);
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, ServiceBusSendFailureClassifier>());
        configurator.SetBusFactory(new ServiceBusRegistrationBusFactory(configure));

        AddSubscriptionEndpointConnector<TBus>(configurator.Services);
    }

    static void AddTransportOptions(IServiceCollection services, string optionsName, string bus)
    {
        services.AddOptions<AzureServiceBusTransportOptions>(optionsName)
            .Validate(
                static options => options.ConnectionString is null || !string.IsNullOrWhiteSpace(options.ConnectionString),
                $"Azure Service Bus transport for bus '{bus}': ConnectionString must not be empty when specified. Set a complete connection string or leave it unset and configure the host in the bus callback.")
            .ValidateOnStart();
    }

    static void AddSubscriptionEndpointConnector<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        services.TryAddSingleton(provider =>
        {
            var subscriptionEndpointConnector = provider.GetRequiredService<IBusInstance<TBus>>().BusInstance as ISubscriptionEndpointConnector;

            return subscriptionEndpointConnector ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", "The default bus instance is not an Azure Service Bus Instance", "Correct the named configuration before starting the host"));
        });

        services.TryAddSingleton(provider =>
        {
            var subscriptionEndpointConnector = provider.GetRequiredService<IBusInstance<TBus>>().BusInstance as ISubscriptionEndpointConnector;

            return Bind<TBus>.Create(subscriptionEndpointConnector
                ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", "The default bus instance is not an Azure Service Bus Instance", "Correct the named configuration before starting the host")));
        });
    }
}
