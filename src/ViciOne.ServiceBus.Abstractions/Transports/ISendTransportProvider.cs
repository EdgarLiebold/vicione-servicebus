using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface ISendTransportProvider
{
    Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default);

    Uri NormalizeAddress(Uri address);
}
