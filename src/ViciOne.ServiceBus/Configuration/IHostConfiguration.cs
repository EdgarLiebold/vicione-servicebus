using System;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for host configuration.
/// </summary>
public interface IHostConfiguration :
    IEndpointConfigurationObserverConnector,
    IReceiveObserverConnector,
    IConsumeObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector,
    ISpecification
{
    /// <summary>
    /// Gets the bus configuration value.
    /// </summary>
    IBusConfiguration BusConfiguration { get; }

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    Uri HostAddress { get; }

    /// <summary>
    /// If true, only the broker topology will be deployed
    /// </summary>
    bool DeployTopologyOnly { get; set; }

    /// <summary>
    /// If true, the publish topology will be deployed at startup
    /// </summary>
    bool DeployPublishTopology { get; set; }

    /// <summary>
    /// Gets the send observers value.
    /// </summary>
    ISendObserver SendObservers { get; }

    /// <summary>
    /// Gets or sets the log context value.
    /// </summary>
    ILogContext? LogContext { get; set; }
    /// <summary>
    /// Gets the receive log context value.
    /// </summary>
    ILogContext? ReceiveLogContext { get; }
    /// <summary>
    /// Gets the send log context value.
    /// </summary>
    ILogContext? SendLogContext { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    IBusTopology Topology { get; }

    /// <summary>
    /// Gets the receive transport retry policy value.
    /// </summary>
    IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>
    /// Gets the send transport retry policy value.
    /// </summary>
    IRetryPolicy SendTransportRetryPolicy { get; }

    /// <summary>
    /// Gets or sets the consumer stop timeout value.
    /// </summary>
    TimeSpan? ConsumerStopTimeout { get; set; }
    /// <summary>
    /// Gets or sets the stop timeout value.
    /// </summary>
    TimeSpan? StopTimeout { get; set; }

    /// <summary>
    /// Create a receive endpoint configuration
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName, Action<IReceiveEndpointConfigurator>? configure = null);

    /// <summary>
    /// Called by the base ReceiveEndpointContext constructor so that the observer collections are connected to the bus observer
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    ConnectHandle ConnectReceiveEndpointContext(ReceiveEndpointContext context);

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IHost Build();
}
