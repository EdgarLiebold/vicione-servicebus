using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Advanced scheduler construction hooks. Application code should register a scheduler with the
/// service-bus configurator and inject <see cref="IMessageScheduler"/>.
/// </summary>
public static class MessageSchedulerBusExtensions
{
    /// <summary>
    /// Create a message scheduler that uses an external message scheduler, such as Quartz.NET, to
    /// schedule messages. This should not be used with the broker-specific message schedulers.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="bus">The bus.</param>
    /// <param name="schedulerEndpointAddress">The endpoint address of the scheduler service.</param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns>The created message scheduler.</returns>
    public static IMessageScheduler CreateMessageScheduler(this IBus bus, Uri schedulerEndpointAddress, TimeProvider? timeProvider = null)
    {
        Task<ISendEndpoint> GetSchedulerEndpointAsync(CancellationToken cancellationToken)
        {
            return bus.GetSendEndpointAsync(schedulerEndpointAddress, cancellationToken: cancellationToken);
        }

        return new MessageScheduler(new EndpointScheduleMessageProvider(GetSchedulerEndpointAsync, timeProvider), bus.Topology, timeProvider);
    }

    /// <summary>
    /// Create a message scheduler that uses an external message scheduler, such as Quartz.NET, to
    /// schedule messages. This should not be used with the broker-specific message schedulers.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    /// <param name="busTopology">The bus topology.</param>
    /// <param name="schedulerEndpointAddress">The endpoint address of the scheduler service.</param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns>The created message scheduler.</returns>
    public static IMessageScheduler CreateMessageScheduler(this ISendEndpointProvider sendEndpointProvider, IBusTopology busTopology,
        Uri schedulerEndpointAddress, TimeProvider? timeProvider = null)
    {
        Task<ISendEndpoint> GetSchedulerEndpointAsync(CancellationToken cancellationToken)
        {
            return sendEndpointProvider.GetSendEndpointAsync(schedulerEndpointAddress, cancellationToken: cancellationToken);
        }

        return new MessageScheduler(new EndpointScheduleMessageProvider(GetSchedulerEndpointAsync, timeProvider), busTopology, timeProvider);
    }

    /// <summary>
    /// Create a message scheduler that uses an external message scheduler, such as Quartz.NET, to
    /// schedule messages. This should not be used with the broker-specific message schedulers. Scheduled messages
    /// are published to the external message scheduler, rather than uses a preconfigured endpoint address.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="bus">The bus.</param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns>The created message scheduler.</returns>
    public static IMessageScheduler CreateMessageScheduler(this IBus bus, TimeProvider? timeProvider = null)
    {
        return new MessageScheduler(new PublishScheduleMessageProvider(bus, timeProvider), bus.Topology, timeProvider);
    }

    /// <summary>
    /// Create a message scheduler that uses an external message scheduler, such as Quartz.NET, to
    /// schedule messages. This should not be used with the broker-specific message schedulers. Scheduled messages
    /// are published to the external message scheduler, rather than uses a preconfigured endpoint address.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="busTopology">The bus topology.</param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns>The created message scheduler.</returns>
    public static IMessageScheduler CreateMessageScheduler(this IPublishEndpoint publishEndpoint, IBusTopology busTopology, TimeProvider? timeProvider = null)
    {
        return new MessageScheduler(new PublishScheduleMessageProvider(publishEndpoint, timeProvider), busTopology, timeProvider);
    }

    /// <summary>
    /// Create a message scheduler that uses the built-in transport message delay to schedule messages.
    /// NOTE that this should only be used to schedule messages outside of a message consumer. Consumers should
    /// use the ScheduleSend extensions on ConsumeContext.
    /// </summary>
    /// <param name="bus">The bus.</param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns>The created delayed message scheduler.</returns>
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
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    /// <param name="busTopology">The bus topology.</param>
    /// <param name="timeProvider">The clock used for relative scheduling operations.</param>
    /// <returns>The created delayed message scheduler.</returns>
    public static IMessageScheduler CreateDelayedMessageScheduler(this ISendEndpointProvider sendEndpointProvider, IBusTopology busTopology, TimeProvider? timeProvider = null)
    {
        TimeProvider schedulerTimeProvider = timeProvider ?? TimeProvider.System;
        return new MessageScheduler(new DelayedScheduleMessageProvider(sendEndpointProvider, schedulerTimeProvider), busTopology,
            schedulerTimeProvider);
    }
}
