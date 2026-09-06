using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides schedule message services.</summary>
public interface IScheduleMessageProvider
{
    /// <summary>Schedule a message to be sent.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class;

    /// <summary>Cancel a scheduled message by TokenId.</summary>
    /// <param name="tokenId">The tokenId of the scheduled message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken);

    /// <summary>Cancel a scheduled message by TokenId.</summary>
    /// <param name="destinationAddress">The destination address of the scheduled message.</param>
    /// <param name="tokenId">The tokenId of the scheduled message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken);
}
