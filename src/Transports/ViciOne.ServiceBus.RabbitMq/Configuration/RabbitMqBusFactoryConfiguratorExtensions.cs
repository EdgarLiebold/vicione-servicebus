using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.RabbitMq;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Operations;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates and registers RabbitMQ-backed bus instances.</summary>
public static class RabbitMqBusFactoryConfiguratorExtensions
{
    /// <summary>Select RabbitMQ as the transport for the service bus.</summary>
    /// <param name="selector">The bus-factory selector.</param>
    /// <param name="configure">An optional RabbitMQ bus configuration callback.</param>
    /// <returns>The configured RabbitMQ bus control.</returns>
    public static IBusControl CreateUsingRabbitMq(this IBusFactorySelector selector, Action<IRabbitMqBusFactoryConfigurator>? configure = null)
    {
        return RabbitMqBusFactory.Create(configure);
    }

    /// <summary>Configure ViciOne.ServiceBus to use RabbitMQ for the transport.</summary>
    /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus).</param>
    /// <param name="configure">The configuration callback for the bus factory.</param>
    public static void UsingRabbitMq(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configure = null)
    {
        AddSharedServices(configurator.Services, string.Empty, "default");
        configurator.Services.TryAddSingleton<IDurableSendDispatcher<IBus>, RabbitMqDurableSendDispatcher<IBus>>();
        configurator.SetBusFactory(new RabbitMqRegistrationBusFactory(configure));
    }

    /// <summary>Configure a typed ViciOne.ServiceBus instance to use RabbitMQ for the transport.</summary>
    /// <typeparam name="TBus">The typed bus contract that owns the transport and Durable Sender.</typeparam>
    /// <param name="configurator">The typed bus registration configurator.</param>
    /// <param name="configure">The configuration callback for the bus factory.</param>
    public static void UsingRabbitMq<TBus>(this IBusRegistrationConfigurator<TBus> configurator,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configure = null)
        where TBus : class, IBus
    {
        AddSharedServices(configurator.Services, typeof(TBus).Name, typeof(TBus).FullName ?? typeof(TBus).Name);
        configurator.Services.TryAddSingleton<IDurableSendDispatcher<TBus>, RabbitMqDurableSendDispatcher<TBus>>();
        configurator.SetBusFactory(new RabbitMqRegistrationBusFactory(configure));
    }

    static void AddSharedServices(IServiceCollection services, string optionsName, string bus)
    {
        services.AddOptions<RabbitMqTransportOptions>(optionsName)
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.Host),
                $"RabbitMQ transport for bus '{bus}': Host must not be empty. Set a resolvable broker host name.")
            .Validate(
                static options => options.Port > 0,
                $"RabbitMQ transport for bus '{bus}': Port must be between 1 and 65535. Set a valid AMQP port.")
            .Validate(
                static options => options.ManagementPort > 0,
                $"RabbitMQ transport for bus '{bus}': ManagementPort must be between 1 and 65535. Set a valid management API port.")
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.VHost),
                $"RabbitMQ transport for bus '{bus}': VHost must not be empty. Set an explicit virtual-host path.")
            .Validate(
                static options => options.User is not null && options.Pass is not null,
                $"RabbitMQ transport for bus '{bus}': User and Pass must not be null. Set credentials or explicit empty strings when anonymous access is intended.")
            .ValidateOnStart();
        services.AddOptions<RabbitMqSslOptions>(optionsName)
            .Validate(
                static options => HasOnlyDefinedProtocolFlags(options.Protocol),
                $"RabbitMQ TLS for bus '{bus}': Protocol contains undefined flags. Select only supported SslProtocols values or None for the system default.")
            .Validate(
                static options => options.CertPassphrase is null || !string.IsNullOrWhiteSpace(options.CertPath),
                $"RabbitMQ TLS for bus '{bus}': CertPassphrase is set without CertPath. Set the certificate path or remove the passphrase.")
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, RabbitMqSendFailureClassifier>());
        services.TryAddSingleton<IRabbitMqQueueOperations, RabbitMqQueueOperations>();
        services.TryAddSingleton(typeof(IRabbitMqQueueOperations<>), typeof(RabbitMqQueueOperations<>));
    }

    static bool HasOnlyDefinedProtocolFlags(System.Security.Authentication.SslProtocols protocol)
    {
        System.Security.Authentication.SslProtocols known = System.Security.Authentication.SslProtocols.None;
        foreach (System.Security.Authentication.SslProtocols value in Enum.GetValues<System.Security.Authentication.SslProtocols>())
            known |= value;

        return (protocol & ~known) == 0;
    }
}
