using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Scheduling;

#nullable enable annotations
namespace ViciOne.ServiceBus;

/// <summary>
/// Advanced scheduler construction hooks. Application code should register a scheduler with the
/// service-bus configurator and inject <see cref="IMessageScheduler"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessageSchedulerBusExtensions
{
    /// <summary>
    /// Create a message scheduler that uses an external message scheduler, such as Quartz.NET, to
    /// schedule messages. This should not be used with the broker-specific message schedulers.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="bus"></param>
    /// <param name="schedulerEndpointAddress">The endpoint address of the scheduler service</param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns></returns>
    public static IMessageScheduler CreateMessageScheduler(this IBus bus, Uri schedulerEndpointAddress, TimeProvider? timeProvider = null)
    {
        Task<ISendEndpoint> GetSchedulerEndpoint()
        {
            return bus.GetSendEndpoint(schedulerEndpointAddress);
        }

        return new MessageScheduler(new EndpointScheduleMessageProvider(GetSchedulerEndpoint), bus.Topology, timeProvider);
    }

    /// <summary>
    /// Create a message scheduler that uses an external message scheduler, such as Quartz.NET, to
    /// schedule messages. This should not be used with the broker-specific message schedulers.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="busTopology"></param>
    /// <param name="schedulerEndpointAddress">The endpoint address of the scheduler service</param>
    /// <param name="sendEndpointProvider"></param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns></returns>
    public static IMessageScheduler CreateMessageScheduler(this ISendEndpointProvider sendEndpointProvider, IBusTopology busTopology,
        Uri schedulerEndpointAddress, TimeProvider? timeProvider = null)
    {
        Task<ISendEndpoint> GetSchedulerEndpoint()
        {
            return sendEndpointProvider.GetSendEndpoint(schedulerEndpointAddress);
        }

        return new MessageScheduler(new EndpointScheduleMessageProvider(GetSchedulerEndpoint), busTopology, timeProvider);
    }

    /// <summary>
    /// Create a message scheduler that uses an external message scheduler, such as Quartz.NET, to
    /// schedule messages. This should not be used with the broker-specific message schedulers. Scheduled messages
    /// are published to the external message scheduler, rather than uses a preconfigured endpoint address.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="bus"></param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns></returns>
    public static IMessageScheduler CreateMessageScheduler(this IBus bus, TimeProvider? timeProvider = null)
    {
        return new MessageScheduler(new PublishScheduleMessageProvider(bus), bus.Topology, timeProvider);
    }

    /// <summary>
    /// Create a message scheduler that uses an external message scheduler, such as Quartz.NET, to
    /// schedule messages. This should not be used with the broker-specific message schedulers. Scheduled messages
    /// are published to the external message scheduler, rather than uses a preconfigured endpoint address.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="busTopology"></param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns></returns>
    public static IMessageScheduler CreateMessageScheduler(this IPublishEndpoint publishEndpoint, IBusTopology busTopology, TimeProvider? timeProvider = null)
    {
        return new MessageScheduler(new PublishScheduleMessageProvider(publishEndpoint), busTopology, timeProvider);
    }

    /// <summary>
    /// Create a message scheduler that uses the built-in transport message delay to schedule messages.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="bus"></param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns></returns>
    public static IMessageScheduler CreateDelayedMessageScheduler(this IBus bus, TimeProvider? timeProvider = null)
    {
        TimeProvider schedulerTimeProvider = timeProvider ?? TimeProvider.System;
        return new MessageScheduler(new DelayedScheduleMessageProvider(bus, schedulerTimeProvider), bus.Topology, schedulerTimeProvider);
    }

    /// <summary>
    /// Create a message scheduler that uses the built-in transport message delay to schedule messages.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="sendEndpointProvider"></param>
    /// <param name="busTopology"></param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns></returns>
    public static IMessageScheduler CreateDelayedMessageScheduler(this ISendEndpointProvider sendEndpointProvider, IBusTopology busTopology, TimeProvider? timeProvider = null)
    {
        TimeProvider schedulerTimeProvider = timeProvider ?? TimeProvider.System;
        return new MessageScheduler(new DelayedScheduleMessageProvider(sendEndpointProvider, schedulerTimeProvider), busTopology,
            schedulerTimeProvider);
    }
}
