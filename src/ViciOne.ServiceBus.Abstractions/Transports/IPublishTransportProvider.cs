using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface IPublishTransportProvider
{
    Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class;
}
