using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

public interface IRiderControl :
    IRider
{
    RiderHandle Start(CancellationToken cancellationToken = default);

    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
