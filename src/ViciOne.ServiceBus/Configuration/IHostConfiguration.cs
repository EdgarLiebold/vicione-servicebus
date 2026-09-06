using System;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines host configuration.</summary>
public interface IHostConfiguration :
    IEndpointConfigurationObserverConnector,
    IReceiveObserverConnector,
    IConsumeObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector,
    ISpecification
{
    /// <summary>Gets the bus configuration.</summary>
    IBusConfiguration BusConfiguration { get; }

    /// <summary>Gets the host address.</summary>
    Uri HostAddress { get; }

    /// <summary>If true, only the broker topology will be deployed.</summary>
    bool DeployTopologyOnly { get; set; }

    /// <summary>If true, the publish topology will be deployed at startup.</summary>
    bool DeployPublishTopology { get; set; }

    /// <summary>Gets the send observers.</summary>
    ISendObserver SendObservers { get; }

    /// <summary>Gets or sets the log context.</summary>
    ILogContext? LogContext { get; set; }
    /// <summary>Gets the receive log context.</summary>
    ILogContext? ReceiveLogContext { get; }
    /// <summary>Gets the send log context.</summary>
    ILogContext? SendLogContext { get; }

    /// <summary>Gets the topology.</summary>
    IBusTopology Topology { get; }

    /// <summary>Gets the receive transport retry policy.</summary>
    IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>Gets the send transport retry policy.</summary>
    IRetryPolicy SendTransportRetryPolicy { get; }

    /// <summary>Gets or sets the consumer stop timeout.</summary>
    TimeSpan? ConsumerStopTimeout { get; set; }
    /// <summary>Gets or sets the stop timeout.</summary>
    TimeSpan? StopTimeout { get; set; }

    /// <summary>Create a receive endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created receive endpoint configuration.</returns>
    IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName, Action<IReceiveEndpointConfigurator>? configure = null);

    /// <summary>Called by the base ReceiveEndpointContext constructor so that the observer collections are connected to the bus observer.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectReceiveEndpointContext(ReceiveEndpointContext context);

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    IHost Build();
}
