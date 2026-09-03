namespace ViciOne.ServiceBus
{
    using System;
    using Quartz;
    using Quartz.Spi;


    internal sealed class QuartzSchedulerSettings
    {
        public QuartzSchedulerSettings(ISchedulerFactory schedulerFactory, string queueName,
            Func<IBus, TimeProvider, IJobFactory>? createJobFactory, bool startScheduler, TimeProvider timeProvider,
            Func<string, TimeZoneInfo?>? timeZoneResolver)
        {
            SchedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
            TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

            QueueName = queueName;
            CreateJobFactory = createJobFactory;
            StartScheduler = startScheduler;
            TimeZoneResolver = timeZoneResolver;
        }

        public ISchedulerFactory SchedulerFactory { get; }
        public string QueueName { get; }
        public Func<IBus, TimeProvider, IJobFactory>? CreateJobFactory { get; }
        public bool StartScheduler { get; }
        public TimeProvider TimeProvider { get; }
        public Func<string, TimeZoneInfo?>? TimeZoneResolver { get; }
    }
}
