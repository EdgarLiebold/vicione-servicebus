using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Dispatches a prepared <see cref="ReceiveContext" /> to a <see cref="IReceivePipe" />.</summary>
public interface IReceivePipeDispatcher :
    IConsumePipeConnector,
    IConsumeObserverConnector,
    IConsumeMessageObserverConnector,
    IRequestPipeConnector,
    IDispatchMetrics,
    IReceiveObserverConnector,
    IProbeSite
{
    /// <summary>Dispatches one received message and settles its transport lock.</summary>
    /// <param name="context">The received-message context passed to observers and the pipeline.</param>
    /// <param name="receiveLock">The transport lock completed or faulted with the dispatch outcome.</param>
    /// <param name="cancellationToken">The token that cancels lock validation and settlement.</param>
    /// <returns>A task that completes after dispatch, settlement, and activity notification.</returns>
    Task DispatchAsync(ReceiveContext context, ReceiveLockContext receiveLock, CancellationToken cancellationToken = default);
}
