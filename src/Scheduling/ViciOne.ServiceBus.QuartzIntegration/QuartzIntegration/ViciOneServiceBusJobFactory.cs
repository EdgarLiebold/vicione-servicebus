// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.QuartzIntegration
{
    using System;
    using Quartz;
    using Quartz.Spi;


    public class ViciOneServiceBusJobFactory :
        IJobFactory
    {
        readonly IBus _bus;

        public ViciOneServiceBusJobFactory(IBus bus)
        {
            _bus = bus;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return new ScheduledMessageJob(_bus);
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}
