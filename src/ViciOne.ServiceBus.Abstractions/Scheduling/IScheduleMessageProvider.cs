using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for schedule message provider.
/// </summary>
public interface IScheduleMessageProvider
{
    /// <summary>
    /// Schedule a message to be sent
    /// </summary>
    /// <param name="destinationAddress"></param>
    /// <param name="dueAt"></param>
    /// <param name="message"></param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// Cancel a scheduled message by TokenId
    /// </summary>
    /// <param name="tokenId">The tokenId of the scheduled message</param>
    /// <param name="cancellationToken"></param>
    Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancel a scheduled message by TokenId
    /// </summary>
    /// <param name="destinationAddress">The destination address of the scheduled message</param>
    /// <param name="tokenId">The tokenId of the scheduled message</param>
    /// <param name="cancellationToken"></param>
    Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken);
}
