// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading.Tasks;


    /// <summary>
    /// If present, can be used to move the <see cref="ReceiveContext" /> to the dead letter queue
    /// </summary>
    public interface IDeadLetterTransport
    {
        /// <summary>
        /// Writes the message to the dead letter queue, adding the reason as a transport header
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        Task Send(ReceiveContext context, string reason);
    }
}
