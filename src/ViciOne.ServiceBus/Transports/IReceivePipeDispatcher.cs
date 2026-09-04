using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Dispatches a prepared <see cref="ReceiveContext" /> to a <see cref="IReceivePipe" />.
/// </summary>
public interface IReceivePipeDispatcher :
    IConsumePipeConnector,
    IConsumeObserverConnector,
    IConsumeMessageObserverConnector,
    IRequestPipeConnector,
    IDispatchMetrics,
    IReceiveObserverConnector,
    IProbeSite
{
    /// <summary>
    /// Performs the dispatch operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="receiveLock">The receive lock value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DispatchAsync(ReceiveContext context, ReceiveLockContext receiveLock, CancellationToken cancellationToken = default);
}
