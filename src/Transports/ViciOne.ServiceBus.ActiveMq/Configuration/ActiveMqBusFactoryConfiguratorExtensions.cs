using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for active mq bus factory configurator.
/// </summary>
public static class ActiveMqBusFactoryConfiguratorExtensions
{
    /// <summary>
    /// Select ActiveMQ as the transport for the service bus
    /// </summary>
    public static IBusControl CreateUsingActiveMq(this IBusFactorySelector selector, Action<IActiveMqBusFactoryConfigurator> configure)
    {
        return ActiveMqBusFactory.Create(configure);
    }

    /// <summary>
    /// Configure ViciOne.ServiceBus to use ActiveMQ for the transport.
    /// </summary>
    /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
    /// <param name="configure">The configuration callback for the bus factory</param>
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
