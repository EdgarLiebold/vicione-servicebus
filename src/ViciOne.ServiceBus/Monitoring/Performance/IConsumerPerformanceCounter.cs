namespace ViciOne.ServiceBus.Monitoring.Performance
{
    using System;


    public interface IConsumerPerformanceCounter
    {
        void Consumed(TimeSpan duration);
        void Faulted();
    }
}
