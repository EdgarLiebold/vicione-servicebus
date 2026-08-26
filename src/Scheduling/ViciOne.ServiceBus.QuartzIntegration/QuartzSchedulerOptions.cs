namespace ViciOne.ServiceBus
{
    using System;
    using Quartz;
    using Quartz.Impl;
    using Quartz.Spi;


    public class QuartzSchedulerOptions
    {
        /// <summary>
        /// Used to create the scheduler at bus start, defaults to <see cref="StdSchedulerFactory" />.
        /// </summary>
        public ISchedulerFactory SchedulerFactory { get; set; } = new StdSchedulerFactory();

        /// <summary>
        /// The queue name for the quartz service, defaults to "quartz".
        /// </summary>
        public string QueueName { get; set; } = "quartz";

        /// <summary>
        /// Only supported when configuring the in-memory scheduler to inject the ViciOneServiceBusJobFactory
        /// when not using a container.
        /// </summary>
        public Func<IBus, TimeProvider, IJobFactory>? CreateJobFactory { get; set; }

        /// <summary>
        /// Whether to start the scheduler when bus starts, defaults to true.
        /// </summary>
        public bool StartScheduler { get; set; } = true;

        /// <summary>
        /// Provides the single UTC time source used by ViciOne scheduling jobs. The default is
        /// <see cref="TimeProvider.System" />.
        /// </summary>
        public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

        internal QuartzSchedulerSettings CreateSettings()
        {
            return new QuartzSchedulerSettings(SchedulerFactory, QueueName, CreateJobFactory, StartScheduler, TimeProvider);
        }
    }
}
