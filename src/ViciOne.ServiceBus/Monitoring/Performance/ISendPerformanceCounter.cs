// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Monitoring.Performance
{
    public interface ISendPerformanceCounter
    {
        void Sent();
        void Faulted();
    }
}
