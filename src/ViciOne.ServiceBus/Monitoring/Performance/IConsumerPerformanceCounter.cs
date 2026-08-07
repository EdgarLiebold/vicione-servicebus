// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Monitoring.Performance
{
    using System;


    public interface IConsumerPerformanceCounter
    {
        void Consumed(TimeSpan duration);
        void Faulted();
    }
}
