// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// A handle to an active transport
    /// </summary>
    public interface ReceiveTransportHandle
    {
        /// <summary>
        /// Stop the transport, releasing any resources associated with the endpoint
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Stop(CancellationToken cancellationToken = default);
    }
}
