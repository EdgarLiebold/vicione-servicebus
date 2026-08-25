namespace ViciOne.ServiceBus.Clients
{
    using System;
    using System.Threading.Tasks;


    public class HostReceiveEndpointClientFactoryContext :
        ReceiveEndpointClientFactoryContext,
        IAsyncDisposable
    {
        readonly HostReceiveEndpointHandle _handle;

        public HostReceiveEndpointClientFactoryContext(
            HostReceiveEndpointHandle handle,
            RequestTimeout defaultTimeout = default,
            TimeProvider timeProvider = null)
            : base(handle, defaultTimeout, timeProvider)
        {
            _handle = handle;
        }

        public async ValueTask DisposeAsync()
        {
            await _handle.StopAsync().ConfigureAwait(false);
        }
    }
}
