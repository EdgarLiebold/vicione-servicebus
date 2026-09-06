using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines receive endpoint configuration.</summary>
public interface IReceiveEndpointConfiguration :
    IEndpointConfiguration,
    IReceiveEndpointObserverConnector,
    IReceiveEndpointDependencyConnector,
    IReceiveEndpointDependentConnector
{
    /// <summary>Gets the consume pipe.</summary>
    IConsumePipe ConsumePipe { get; }

    /// <summary>Gets the host address.</summary>
    Uri HostAddress { get; }

    /// <summary>Gets the input address.</summary>
    Uri InputAddress { get; }

    /// <summary>Gets or sets the configure consume topology.</summary>
    bool ConfigureConsumeTopology { get; set; }

    /// <summary>Gets or sets the publish faults.</summary>
    bool PublishFaults { get; set; }

    /// <summary>Gets the prefetch count.</summary>
    int PrefetchCount { get; }

    /// <summary>Gets the concurrent message limit.</summary>
    int? ConcurrentMessageLimit { get; }

    /// <summary>Gets the endpoint observers.</summary>
    ReceiveEndpointObservable EndpointObservers { get; }
    /// <summary>Gets the receive observers.</summary>
    ReceiveObservable ReceiveObservers { get; }
    /// <summary>Gets the transport observers.</summary>
    ReceiveTransportObservable TransportObservers { get; }
    /// <summary>Gets the receive endpoint.</summary>
    IReceiveEndpoint ReceiveEndpoint { get; }

    /// <summary>Configures whether consume topology is created for a message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="enabled">Whether topology creation is enabled.</param>
    void ConfigureMessageTopology<T>(bool enabled = true)
        where T : class;

    /// <summary>Configures whether consume topology is created for a message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="enabled">Whether topology creation is enabled.</param>
    void ConfigureMessageTopology(Type messageType, bool enabled = true);

    /// <summary>Completed once the receive endpoint dependencies are ready.</summary>
    Task DependenciesReady { get; }

    /// <summary>Completed once the receive endpoint dependents are completed.</summary>
    Task DependentsCompleted { get; }

    /// <summary>Create the receive pipe, using the endpoint configuration.</summary>
    /// <returns>The created receive pipe.</returns>
    IReceivePipe CreateReceivePipe();

    /// <summary>Creates receive endpoint context.</summary>
    /// <returns>The created receive endpoint context.</returns>
    ReceiveEndpointContext CreateReceiveEndpointContext();
}
