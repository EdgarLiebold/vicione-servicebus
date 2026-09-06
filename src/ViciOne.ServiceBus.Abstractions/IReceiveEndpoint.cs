using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>A service endpoint has an inbound transport that pushes messages to consumers.</summary>
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
    /// <summary>Gets the input address.</summary>
    Uri InputAddress { get; }

    /// <summary>Gets the started.</summary>
    Task<ReceiveEndpointReady> Started { get; }

    /// <summary>Starts the receive endpoint.</summary>
    /// <param name="cancellationToken">Cancel the start operation in progress.</param>
    /// <returns>A handle that exposes endpoint readiness and controls its lifetime.</returns>
    ReceiveEndpointHandle Start(CancellationToken cancellationToken = default);

    /// <summary>Stop the receive endpoint.</summary>
    /// <param name="cancellationToken">Cancel the stop operation in progress.</param>
    /// <returns>An awaitable task that is completed once everything is stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
