// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading.Tasks;


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
        Task Dispatch(ReceiveContext context, ReceiveLockContext receiveLock);
    }
}
