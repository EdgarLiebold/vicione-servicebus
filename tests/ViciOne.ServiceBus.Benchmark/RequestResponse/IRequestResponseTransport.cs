// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOneServiceBusBenchmark.RequestResponse
{
    using System;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus;


    public interface IRequestResponseTransport :
        IDisposable
    {
        Task<IRequestClient<T>> GetRequestClient<T>(TimeSpan settingsRequestTimeout)
            where T : class;

        void GetBusControl(Action<IReceiveEndpointConfigurator> callback);
    }
}
