using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;

#nullable enable
namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// The context of a receive endpoint
/// </summary>
public interface ReceiveEndpointContext :
    PipeContext,
    ISendObserverConnector,
    IPublishObserverConnector,
    IReceiveTransportObserverConnector,
    IReceiveObserverConnector,
    IReceiveEndpointObserverConnector,
    IProbeSite
{
    /// <summary>
    /// Gets the consumer stop timeout value.
    /// </summary>
    TimeSpan? ConsumerStopTimeout { get; }
    /// <summary>
    /// Gets the stop timeout value.
    /// </summary>
    TimeSpan? StopTimeout { get; }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    Uri InputAddress { get; }

    /// <summary>
    /// Gets the is bus endpoint value.
    /// </summary>
    bool IsBusEndpoint { get; }

    /// <summary>
    /// Gets the endpoint observers value.
    /// </summary>
    IReceiveEndpointObserver EndpointObservers { get; }

    /// <summary>
    /// Gets the receive observers value.
    /// </summary>
    IReceiveObserver ReceiveObservers { get; }

    /// <summary>
    /// Gets the transport observers value.
    /// </summary>
    IReceiveTransportObserver TransportObservers { get; }

    /// <summary>
    /// Gets the log context value.
    /// </summary>
    ILogContext LogContext { get; }

    /// <summary>
    /// Gets the publish value.
    /// </summary>
    IPublishTopology Publish { get; }

    /// <summary>
    /// Gets the receive pipe value.
    /// </summary>
    IReceivePipe ReceivePipe { get; }

    /// <summary>
    /// Gets the publish endpoint provider value.
    /// </summary>
    IPublishEndpointProvider PublishEndpointProvider { get; }

    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    ISendEndpointProvider SendEndpointProvider { get; }

    /// <summary>
    /// Gets the message routes value.
    /// </summary>
    IMessageRouteTable MessageRoutes { get; }

    /// <summary>
    /// Task completed when dependencies are ready
    /// </summary>
    Task DependenciesReady { get; }

    /// <summary>
    /// Task completed when dependants are completed
    /// </summary>
    Task DependentsCompleted { get; }

    /// <summary>
    /// If true (the default), faults should be published when no ResponseAddress or FaultAddress are present.
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
    /// Gets the serialization value.
    /// </summary>
    ISerialization Serialization { get; }

    /// <summary>
    /// Convert an unknown exception to a <see cref="ConnectionException" />, so that it can be used by
    /// the transport retry policy.
    /// </summary>
    /// <param name="exception">The original exception</param>
    /// <param name="message">A contextual message describing when the exception occurred</param>
    /// <returns></returns>
    Exception ConvertException(Exception exception, string message);

    /// <summary>
    /// Creates receive pipe dispatcher.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IReceivePipeDispatcher CreateReceivePipeDispatcher();

    /// <summary>
    /// Reset the receive endpoint, which should clear any caches, etc.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    ValueTask ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add an consume-side agent, which should be stopped during shutdown
    /// </summary>
    /// <param name="agent"></param>
    void AddConsumeAgent(IAgent agent);

    /// <summary>
    /// Add an agent, which should be stopped during shutdown after consume/send agents have been stopped
    /// </summary>
    /// <param name="agent"></param>
    void AddSendAgent(IAgent agent);
}
