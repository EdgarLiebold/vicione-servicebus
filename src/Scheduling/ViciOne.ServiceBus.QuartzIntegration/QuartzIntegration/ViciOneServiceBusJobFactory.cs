namespace ViciOne.ServiceBus.QuartzIntegration
{
    using System;
    using Quartz;
    using Quartz.Spi;


    public class ViciOneServiceBusJobFactory :
        IJobFactory
    {
        readonly IBus _bus;
        readonly TimeProvider _timeProvider;

        public ViciOneServiceBusJobFactory(IBus bus, TimeProvider timeProvider)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            ArgumentNullException.ThrowIfNull(bundle);
            ArgumentNullException.ThrowIfNull(scheduler);

            return new ScheduledMessageJob(_bus, _timeProvider);
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}
