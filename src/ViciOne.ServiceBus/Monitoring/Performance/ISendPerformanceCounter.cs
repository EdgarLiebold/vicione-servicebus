namespace ViciOne.ServiceBus.Monitoring.Performance
{
    public interface ISendPerformanceCounter
    {
        void Sent();
        void Faulted();
    }
}
