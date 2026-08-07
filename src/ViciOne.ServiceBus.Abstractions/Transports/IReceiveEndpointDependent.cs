// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading.Tasks;


    public interface IReceiveEndpointDependent
    {
        /// <summary>
        /// The task which is completed once the receive endpoint is completed
        /// </summary>
        Task Completed { get; }
    }
}
