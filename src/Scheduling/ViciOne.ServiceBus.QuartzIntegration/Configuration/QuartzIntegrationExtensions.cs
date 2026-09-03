namespace ViciOne.ServiceBus
{
    using System;
    using System.Collections.Specialized;
    using Microsoft.Extensions.DependencyInjection;
    using Quartz;
    using Quartz.Impl;
    using Quartz.Spi;
    using QuartzIntegration;
    using Scheduling;
    using Util;


    public static class QuartzIntegrationExtensions
    {

        public static Uri UseInMemoryScheduler(this IBusFactoryConfigurator configurator, string queueName = "quartz")
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            var schedulerFactory = new StdSchedulerFactory(GetDefaultConfiguration());

            return configurator.UseInMemoryScheduler(schedulerFactory, queueName);
        }

        public static Uri UseInMemoryScheduler(this IBusFactoryConfigurator configurator, out ISchedulerFactory schedulerFactory, string queueName = "quartz")
        {
            schedulerFactory = new StdSchedulerFactory(GetDefaultConfiguration());

            return UseInMemoryScheduler(configurator, schedulerFactory, queueName);
        }

        public static Uri UseInMemoryScheduler(this IBusFactoryConfigurator configurator, ISchedulerFactory schedulerFactory, string queueName = "quartz")
        {
            return configurator.UseInMemoryScheduler(options =>
            {
                options.SchedulerFactory = schedulerFactory;

                options.CreateJobFactory = static (bus, timeProvider) => new ViciOneServiceBusJobFactory(bus, timeProvider);
                options.QueueName = queueName;
            });
        }

        static NameValueCollection GetDefaultConfiguration()
        {
            var configuration = new NameValueCollection
            {
                { "quartz.scheduler.instanceName", $"ViciOne.ServiceBus-{NewId.Next().ToString(FormatUtil.Formatter)}" },
                { "quartz.threadPool.maxConcurrency", Environment.ProcessorCount.ToString("F0") }
            };

            return configuration;
        }

        public static Uri UseInMemoryScheduler(this IBusFactoryConfigurator configurator, Action<QuartzSchedulerOptions>? configure)
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            var options = new QuartzSchedulerOptions();
            configure?.Invoke(options);
            QuartzSchedulerSettings settings = options.CreateSettings();

            Uri? inputAddress = null;

            var observer = new SchedulerBusObserver(settings);

            configurator.ReceiveEndpoint(settings.QueueName, e =>
            {
                var partitioner = configurator.CreatePartitioner(Environment.ProcessorCount);

                e.Consumer(() => new ScheduleMessageConsumer(settings.SchedulerFactory, settings.TimeZoneResolver), x =>
                    x.Message<ScheduleMessage>(m => m.UsePartitioner(partitioner, p => p.Message.TokenId)));

                e.Consumer(() => new CancelScheduledMessageConsumer(settings.SchedulerFactory), x =>
                    x.Message<CancelScheduledMessage>(m => m.UsePartitioner(partitioner, p => p.Message.TokenId)));

                e.Consumer(() => new PauseScheduledMessageConsumer(settings.SchedulerFactory));

                e.Consumer(() => new ResumeScheduledMessageConsumer(settings.SchedulerFactory));

                configurator.UseMessageScheduler(e.InputAddress);

                configurator.ConnectBusObserver(observer);

                inputAddress = e.InputAddress;
            });

            return inputAddress!;
        }
    }
}
