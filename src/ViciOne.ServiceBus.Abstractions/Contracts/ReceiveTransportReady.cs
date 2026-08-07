// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface ReceiveTransportReady :
        ReceiveTransportEvent
    {
        /// <summary>
        /// If true, the receive transport is actually ready, versus "fake-ready" for endpoints which do not auto-start
        /// </summary>
        bool IsStarted { get; }
    }
}
