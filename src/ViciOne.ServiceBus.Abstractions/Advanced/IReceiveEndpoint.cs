using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents an inbound transport and its configured message-consumption pipeline.</summary>
public interface IReceiveEndpoint :
    ISendEndpointProvider,
    IPublishEndpointProvider,
    IConsumePipeConnector,
    IRequestPipeConnector,
    IReceiveObserverConnector,
    IConsumeObserverConnector,
    IConsumeMessageObserverConnector,
    IProbeSite
{
    /// <summary>Gets the transport address from which the endpoint receives messages.</summary>
    Uri InputAddress { get; }

    /// <summary>Gets a task that completes when the endpoint is ready to consume messages.</summary>
    Task<ReceiveEndpointReady> Started { get; }

    /// <summary>Starts the receive endpoint.</summary>
    /// <param name="cancellationToken">The token that cancels endpoint startup.</param>
    /// <returns>A handle that exposes endpoint readiness and controls its lifetime.</returns>
    IReceiveEndpointHandle Start(CancellationToken cancellationToken = default);

    /// <summary>Stops the receive endpoint and releases its transport resources.</summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>A task that completes after the endpoint has stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
