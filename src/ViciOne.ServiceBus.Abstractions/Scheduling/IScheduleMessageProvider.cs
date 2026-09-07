using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the provider SPI for scheduling and canceling messages.</summary>
public interface IScheduleMessageProvider
{
    /// <summary>Schedules a message for delivery to a destination.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destinationAddress">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before the provider accepts the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class;

    /// <summary>Cancels a scheduled message by its token.</summary>
    /// <param name="tokenId">The token that identifies the scheduled message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken);

    /// <summary>Cancels a scheduled message at a specific destination.</summary>
    /// <param name="destinationAddress">The destination that owns the scheduled message.</param>
    /// <param name="tokenId">The token that identifies the scheduled message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken);
}
