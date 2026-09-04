using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

public interface IRiderCollection :
    IAgent
{
    IRider Get(string name);

    void Add(string name, IRiderControl rider);

    HostRiderHandle[] StartRiders(CancellationToken cancellationToken = default);

    HostRiderHandle StartRider(string name, CancellationToken cancellationToken = default);

    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
