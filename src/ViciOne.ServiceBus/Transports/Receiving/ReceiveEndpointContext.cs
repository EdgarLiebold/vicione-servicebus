using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides the pipelines, observers, resources, and lifecycle dependencies owned by a receive endpoint.</summary>
public interface ReceiveEndpointContext :
    PipeContext,
    ISendObserverConnector,
    IPublishObserverConnector,
    IReceiveTransportObserverConnector,
    IReceiveObserverConnector,
    IReceiveEndpointObserverConnector,
    IProbeSite
{
    /// <summary>Gets the maximum graceful wait for active consumers, or <see langword="null" /> for no endpoint-specific limit.</summary>
    TimeSpan? ConsumerStopTimeout { get; }
    /// <summary>Gets the maximum duration of endpoint shutdown, or <see langword="null" /> for no endpoint-specific limit.</summary>
    TimeSpan? StopTimeout { get; }

    /// <summary>Gets the address from which the endpoint receives messages.</summary>
    Uri InputAddress { get; }

    /// <summary>Gets whether this endpoint receives messages addressed to the bus itself.</summary>
    bool IsBusEndpoint { get; }

    /// <summary>Gets the aggregate receive-endpoint lifecycle observer.</summary>
    IReceiveEndpointObserver EndpointObservers { get; }

    /// <summary>Gets the aggregate receive-pipeline observer.</summary>
    IReceiveObserver ReceiveObservers { get; }

    /// <summary>Gets the aggregate receive-transport lifecycle observer.</summary>
    IReceiveTransportObserver TransportObservers { get; }

    /// <summary>Gets the endpoint-scoped diagnostic context.</summary>
    ILogContext LogContext { get; }

    /// <summary>Gets the publish topology visible to the endpoint.</summary>
    IPublishTopology Publish { get; }

    /// <summary>Gets the pipeline that processes received messages.</summary>
    IReceivePipe ReceivePipe { get; }

    /// <summary>Gets the endpoint provider used to publish from the receive pipeline.</summary>
    IPublishEndpointProvider PublishEndpointProvider { get; }

    /// <summary>Gets the endpoint provider used to send from the receive pipeline.</summary>
    ISendEndpointProvider SendEndpointProvider { get; }

    /// <summary>Gets the host message-route table.</summary>
    IMessageRouteTable MessageRoutes { get; }

    /// <summary>Gets the task that completes when endpoint dependencies are ready.</summary>
    Task DependenciesReady { get; }

    /// <summary>Gets the task that completes when endpoint dependents have stopped.</summary>
    Task DependentsCompleted { get; }

    /// <summary>Gets whether unaddressed consumer faults are published.</summary>
    bool PublishFaults { get; }

    /// <summary>Gets the transport prefetch limit.</summary>
    int PrefetchCount { get; }

    /// <summary>Gets the endpoint-wide concurrent message limit, or <see langword="null" /> when unconstrained.</summary>
    int? ConcurrentMessageLimit { get; }

    /// <summary>Gets the serializer collection used by the endpoint.</summary>
    ISerialization Serialization { get; }

    /// <summary>
    /// Convert an unknown exception to a <see cref="ConnectionException" />, so that it can be used by
    /// the transport retry policy.
    /// </summary>
    /// <param name="exception">The original exception.</param>
    /// <param name="message">A contextual message describing when the exception occurred.</param>
    /// <returns>The converted exception.</returns>
    Exception ConvertException(Exception exception, string message);

    /// <summary>Creates an independently metered dispatcher over the endpoint receive pipeline.</summary>
    /// <returns>A new receive-pipeline dispatcher.</returns>
    IReceivePipeDispatcher CreateReceivePipeDispatcher();

    /// <summary>Releases endpoint-provider resources and initializes empty provider caches for the next generation.</summary>
    /// <param name="cancellationToken">The token that cancels resource release.</param>
    /// <returns>A value task that completes after the current providers have been released.</returns>
    ValueTask ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds an agent that must stop with consume-side activity.</summary>
    /// <param name="agent">The consume-side agent owned by the endpoint.</param>
    void AddConsumeAgent(IAgent agent);

    /// <summary>Adds an agent that must stop after consume-side activity has ended.</summary>
    /// <param name="agent">The send-side agent owned by the endpoint.</param>
    void AddSendAgent(IAgent agent);
}
