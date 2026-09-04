using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface IPublishTransportProvider
{
    Task<ISendTransport> GetPublishTransport<T>(Uri? publishAddress)
        where T : class;
}
