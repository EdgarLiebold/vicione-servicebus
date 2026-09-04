using System;
using Quartz;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>
/// Defines configuration options for quartz scheduler.
/// </summary>
public class QuartzSchedulerOptions
{
    /// <summary>
    /// Used to create the scheduler at bus start. The default is an isolated Quartz 4 standalone in-memory scheduler.
    /// </summary>
    public ISchedulerFactory SchedulerFactory { get; set; } = QuartzSchedulerBuilder.Create(builder =>
        builder.UseJobFactory(new Quartz.ViciOneServiceBusJobFactory())).Build();

    /// <summary>
    /// The queue name for the quartz service, defaults to "quartz".
    /// </summary>
    public string QueueName { get; set; } = "quartz";

    /// <summary>
    /// Whether to start the scheduler when bus starts, defaults to true.
    /// </summary>
    public bool StartScheduler { get; set; } = true;

    /// <summary>
    /// Provides the single UTC time source used by ViciOne scheduling jobs. The default is
    /// <see cref="TimeProvider.System" />.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Optional resolver for time zone identifiers not available from the operating system.
    /// </summary>
    public Func<string, TimeZoneInfo?>? TimeZoneResolver { get; set; }

    internal QuartzSchedulerSettings CreateSettings()
    {
        return new QuartzSchedulerSettings(SchedulerFactory, QueueName, StartScheduler, TimeProvider, TimeZoneResolver);
    }
}
