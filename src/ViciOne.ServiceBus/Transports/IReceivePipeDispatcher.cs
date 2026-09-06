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
    /// <summary>Dispatches the current message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="receiveLock">The receive lock.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DispatchAsync(ReceiveContext context, ReceiveLockContext receiveLock, CancellationToken cancellationToken = default);
}
