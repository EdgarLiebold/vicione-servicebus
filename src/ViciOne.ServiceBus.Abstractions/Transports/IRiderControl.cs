// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Collections.Generic;
    using System.Threading;


    public interface IRiderControl :
        IRider
    {
        RiderHandle Start(CancellationToken cancellationToken = default);

        IEnumerable<EndpointHealthResult> CheckEndpointHealth();
    }
}
