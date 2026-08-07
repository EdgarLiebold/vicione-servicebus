// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports.Fabric
{
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// Receives messages from a queue
    /// </summary>
    public interface IMessageReceiver<in T> :
        IProbeSite
        where T : class
    {
        Task Deliver(T message, CancellationToken cancellationToken);
    }
}
