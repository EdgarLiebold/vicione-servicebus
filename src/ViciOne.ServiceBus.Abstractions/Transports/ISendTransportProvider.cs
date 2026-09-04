using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface ISendTransportProvider
{
    Task<ISendTransport> GetSendTransport(Uri address);

    Uri NormalizeAddress(Uri address);
}
