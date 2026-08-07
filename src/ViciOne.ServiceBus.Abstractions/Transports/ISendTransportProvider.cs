// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;


    public interface ISendTransportProvider
    {
        Task<ISendTransport> GetSendTransport(Uri address);

        Uri NormalizeAddress(Uri address);
    }
}
