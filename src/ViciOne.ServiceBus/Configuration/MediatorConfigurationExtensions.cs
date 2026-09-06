using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for mediator configuration.</summary>
public static class MediatorConfigurationExtensions
{
    /// <summary>
    /// Create a mediator, which sends messages to consumers, handlers, and sagas. Messages are dispatched to the consumers asynchronously.
    /// Consumers are not directly coupled to the sender. Can be used entirely in-memory without a broker.
    /// </summary>
    /// <param name="selector">The selector.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created mediator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static IMediator CreateMediator(this IBusFactorySelector selector, Action<IMediatorConfigurator> configure)
    {
        return CreateMediator(selector, null, configure);
    }

    /// <summary>
    /// Create a mediator, which sends messages to consumers, handlers, and sagas. Messages are dispatched to the consumers asynchronously.
    /// Consumers are not directly coupled to the sender. Can be used entirely in-memory without a broker.
    /// </summary>
    /// <param name="selector">The selector.</param>
    /// <param name="baseAddress">The base address.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created mediator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static IMediator CreateMediator(this IBusFactorySelector selector, Uri? baseAddress, Action<IMediatorConfigurator> configure)
    {
        return CreateMediator(selector, baseAddress, configure, TimeProvider.System);
    }

    /// <summary>Create a mediator using an explicit standard .NET time source for request deadlines.</summary>
    /// <param name="selector">The selector.</param>
    /// <param name="baseAddress">The base address.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <returns>The created mediator.</returns>
    public static IMediator CreateMediator(
        this IBusFactorySelector selector,
        Uri? baseAddress,
        Action<IMediatorConfigurator> configure,
        TimeProvider timeProvider)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));
        if (timeProvider == null)
            throw new ArgumentNullException(nameof(timeProvider));

        baseAddress ??= new Uri("loopback://localhost/");
        var topologyConfiguration = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var busConfiguration = new InMemoryBusConfiguration(topologyConfiguration, baseAddress);

        if (LogContext.Current != null)
            busConfiguration.HostConfiguration.LogContext = LogContext.Current;

        var endpointConfiguration = busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration("mediator");

        var configurator = new MediatorConfiguration(busConfiguration.HostConfiguration, endpointConfiguration);

        configure(configurator);

        MessageLimits limits = (busConfiguration.HostConfiguration as IMessageLimitsHostConfiguration)?.MessageLimits
            ?? throw new ConfigurationException(
                "Message limits for bus 'mediator': MaxBodyBytes is not declared. Call mediator.Limits(...) with explicit byte limits.");

        var mediatorDispatcher = configurator.Build();

        var responseEndpointConfiguration = busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration("response");
        var responseConfigurator = new ReceivePipeDispatcherConfiguration(busConfiguration.HostConfiguration, responseEndpointConfiguration);

        configurator = new MediatorConfiguration(busConfiguration.HostConfiguration, responseEndpointConfiguration);

        configure(configurator);

        var responseDispatcher = responseConfigurator.Build();

        return new ViciOneServiceBusMediator(
            LogContext.Current,
            endpointConfiguration,
            mediatorDispatcher,
            responseEndpointConfiguration,
            responseDispatcher,
            limits,
            timeProvider);
    }
}
