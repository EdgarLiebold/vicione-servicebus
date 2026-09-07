using System;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for mediator configuration.</summary>
public static class MediatorConfigurationExtensions
{
    /// <summary>
    /// Creates an in-process mediator that dispatches messages asynchronously without a transport broker.
    /// </summary>
    /// <param name="selector">The bus factory entry point.</param>
    /// <param name="configure">The callback that configures mediator limits and handlers.</param>
    /// <returns>The created mediator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static IMediator CreateMediator(this IBusFactorySelector selector, Action<IMediatorConfigurator> configure)
    {
        return CreateMediator(selector, null, configure);
    }

    /// <summary>
    /// Creates an in-process mediator at the specified loopback base address.
    /// </summary>
    /// <param name="selector">The bus factory entry point.</param>
    /// <param name="baseAddress">The loopback address used as the root for mediator endpoints.</param>
    /// <param name="configure">The callback that configures mediator limits and handlers.</param>
    /// <returns>The created mediator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static IMediator CreateMediator(this IBusFactorySelector selector, Uri? baseAddress, Action<IMediatorConfigurator> configure)
    {
        return CreateMediator(selector, baseAddress, configure, TimeProvider.System);
    }

    /// <summary>Creates an in-process mediator with an explicit time source for request deadlines.</summary>
    /// <param name="selector">The bus factory entry point.</param>
    /// <param name="baseAddress">The loopback address used as the root for mediator endpoints.</param>
    /// <param name="configure">The callback that configures mediator limits and handlers.</param>
    /// <param name="timeProvider">The time source used for request deadlines.</param>
    /// <returns>The created mediator.</returns>
    public static IMediator CreateMediator(
        this IBusFactorySelector selector,
        Uri? baseAddress,
        Action<IMediatorConfigurator> configure,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(timeProvider);

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

        IInMemoryEndpointConfiguration responsePipelineConfiguration =
            InMemoryEndpointConfiguration.CreateChildConfiguration(endpointConfiguration);
        var responseEndpointConfiguration = busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration(
            "response",
            responsePipelineConfiguration);
        var responseConfigurator = new ReceivePipeDispatcherConfiguration(busConfiguration.HostConfiguration, responseEndpointConfiguration);

        var responseDispatcher = responseConfigurator.Build();

        return new InProcessMediator(
            LogContext.Current,
            endpointConfiguration,
            mediatorDispatcher,
            responseEndpointConfiguration,
            responseDispatcher,
            limits,
            timeProvider);
    }
}
