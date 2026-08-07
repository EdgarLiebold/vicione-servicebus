// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;


    public interface IPublishTransportProvider
    {
        Task<ISendTransport> GetPublishTransport<T>(Uri? publishAddress)
            where T : class;
    }
}
