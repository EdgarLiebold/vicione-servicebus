// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Courier.Contracts;


    public interface IRoutingSlipExecutor
    {
        /// <summary>
        /// Execute a routing slip
        /// </summary>
        /// <param name="routingSlip"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Execute(RoutingSlip routingSlip, CancellationToken cancellationToken = default);
    }
}
