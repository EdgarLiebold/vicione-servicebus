using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Mediator.Runtime;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Mediator;

/// <summary>Creates independently owned in-process mediator instances.</summary>
public static class MediatorFactory
{
    /// <summary>Creates an in-process mediator with explicit pipeline configuration.</summary>
    /// <param name="configure">The callback that configures message limits and handlers.</param>
    /// <param name="baseAddress">The optional loopback base address used for mediator endpoints.</param>
    /// <param name="timeProvider">The optional time source used for request deadlines.</param>
    /// <returns>The configured mediator, whose asynchronous lifetime is owned by the caller.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="baseAddress" /> is not an absolute loopback base address.</exception>
    /// <exception cref="ConfigurationException">Message limits are missing or the mediator pipeline is invalid.</exception>
    public static IMediator Create(
        Action<IMediatorConfigurator> configure,
        Uri? baseAddress = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(configure);

        baseAddress = ValidateBaseAddress(baseAddress);
        timeProvider ??= TimeProvider.System;

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

        IReceivePipeDispatcher mediatorDispatcher = configurator.Build();

        IInMemoryEndpointConfiguration responsePipelineConfiguration =
            InMemoryEndpointConfiguration.CreateChildConfiguration(endpointConfiguration);
        IReceiveEndpointConfiguration responseEndpointConfiguration = busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration(
            "response",
            responsePipelineConfiguration);
        var responseConfigurator = new ReceivePipeDispatcherConfiguration(
            busConfiguration.HostConfiguration,
            responseEndpointConfiguration);

        IReceivePipeDispatcher responseDispatcher = responseConfigurator.Build();

        return new InProcessMediator(
            LogContext.Current,
            endpointConfiguration,
            mediatorDispatcher,
            responseEndpointConfiguration,
            responseDispatcher,
            configurator.ConsumeObservers,
            configurator.SendObservers,
            configurator.PublishObservers,
            limits,
            timeProvider);
    }

    internal static Uri ValidateBaseAddress(Uri? baseAddress)
    {
        baseAddress ??= new Uri("loopback://localhost/");

        if (!baseAddress.IsAbsoluteUri)
            throw new ArgumentException("The mediator base address must be absolute.", nameof(baseAddress));
        if (!string.Equals(baseAddress.Scheme, "loopback", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The mediator base address must use the loopback scheme.", nameof(baseAddress));
        if (string.IsNullOrWhiteSpace(baseAddress.Host))
            throw new ArgumentException("The mediator base address must include a host name.", nameof(baseAddress));
        if (!string.IsNullOrEmpty(baseAddress.UserInfo)
            || !string.IsNullOrEmpty(baseAddress.Query)
            || !string.IsNullOrEmpty(baseAddress.Fragment))
        {
            throw new ArgumentException(
                "Credentials, query options, and fragments are not valid mediator base-address components.",
                nameof(baseAddress));
        }

        if (!baseAddress.IsDefaultPort)
            throw new ArgumentException("The mediator base address does not support an explicit port.", nameof(baseAddress));

        return baseAddress;
    }
}
