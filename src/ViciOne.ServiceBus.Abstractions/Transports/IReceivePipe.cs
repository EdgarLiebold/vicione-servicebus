// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading.Tasks;


    public interface IReceivePipe :
        IPipe<ReceiveContext>,
        IConsumePipeConnector,
        IRequestPipeConnector,
        IConsumeMessageObserverConnector,
        IConsumeObserverConnector
    {
        /// <summary>
        /// Task is completed once a connection has been made to the consume pipe (any type of consumer, response handler, etc.
        /// </summary>
        Task Connected { get; }
    }
}
