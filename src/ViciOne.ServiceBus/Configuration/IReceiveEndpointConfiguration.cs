using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for receive endpoint configuration.
/// </summary>
public interface IReceiveEndpointConfiguration :
    IEndpointConfiguration,
    IReceiveEndpointObserverConnector,
    IReceiveEndpointDependentConnector
{
    /// <summary>
    /// Gets the consume pipe value.
    /// </summary>
    IConsumePipe ConsumePipe { get; }

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    Uri HostAddress { get; }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    Uri InputAddress { get; }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    bool ConfigureConsumeTopology { get; }

    /// <summary>
    /// Gets the publish faults value.
    /// </summary>
    bool PublishFaults { get; }

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    int PrefetchCount { get; }

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    int? ConcurrentMessageLimit { get; }

    /// <summary>
    /// Gets the endpoint observers value.
    /// </summary>
    ReceiveEndpointObservable EndpointObservers { get; }
    /// <summary>
    /// Gets the receive observers value.
    /// </summary>
    ReceiveObservable ReceiveObservers { get; }
    /// <summary>
    /// Gets the transport observers value.
    /// </summary>
    ReceiveTransportObservable TransportObservers { get; }
    /// <summary>
    /// Gets the receive endpoint value.
    /// </summary>
    IReceiveEndpoint ReceiveEndpoint { get; }

    /// <summary>
    /// Completed once the receive endpoint dependencies are ready
    /// </summary>
    Task DependenciesReady { get; }

    /// <summary>
    /// Completed once the receive endpoint dependents are completed
    /// </summary>
    Task DependentsCompleted { get; }

    /// <summary>
    /// Create the receive pipe, using the endpoint configuration
    /// </summary>
    /// <returns></returns>
    IReceivePipe CreateReceivePipe();

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    ReceiveEndpointContext CreateReceiveEndpointContext();
}
