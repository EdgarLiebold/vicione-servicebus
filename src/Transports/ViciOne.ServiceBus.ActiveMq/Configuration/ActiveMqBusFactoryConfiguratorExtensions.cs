using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Selects and registers ActiveMQ as a service-bus transport.</summary>
public static class ActiveMqBusFactoryConfiguratorExtensions
{
    /// <summary>Creates a service bus that uses ActiveMQ.</summary>
    /// <param name="selector">The transport selector.</param>
    /// <param name="configure">The callback that configures the ActiveMQ bus.</param>
    /// <returns>The configured bus control.</returns>
    public static IBusControl CreateUsingActiveMq(this IBusFactorySelector selector, Action<IActiveMqBusFactoryConfigurator> configure)
    {
        return ActiveMqBusFactory.Create(configure);
    }

    /// <summary>Registers ActiveMQ transport services and a bus factory.</summary>
    /// <param name="configurator">The service-bus registration configurator.</param>
    /// <param name="configure">An optional callback that configures the bus with its registration context.</param>
    public static void UsingActiveMq(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, IActiveMqBusFactoryConfigurator>? configure = null)
    {
        configurator.Services.AddOptions<ActiveMqTransportOptions>(string.Empty)
            .Validate(
                static options => !HasAnyOption(options) || !string.IsNullOrWhiteSpace(options.Host),
                "ActiveMQ transport for bus 'default': Host is required when transport options are present. Set Host or remove the partial options.")
            .Validate(
                static options => !HasAnyOption(options)
                    || options.Protocol.HasValue && Enum.IsDefined(options.Protocol.Value),
                "ActiveMQ transport for bus 'default': Protocol is required and must be defined when transport options are present. Select OpenWire or Amqp.")
            .Validate(
                static options => !HasAnyOption(options) || options.Port is > 0,
                "ActiveMQ transport for bus 'default': Port is required when transport options are present. Set a port between 1 and 65535.")
            .ValidateOnStart();
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, ActiveMqSendFailureClassifier>());
        configurator.SetBusFactory(new ActiveMqRegistrationBusFactory(configure));
    }

    static bool HasAnyOption(ActiveMqTransportOptions options) =>
        !string.IsNullOrWhiteSpace(options.Host)
        || options.Protocol.HasValue
        || options.Port.HasValue
        || options.UseSsl
        || !string.IsNullOrWhiteSpace(options.User)
        || !string.IsNullOrWhiteSpace(options.Pass);
}
