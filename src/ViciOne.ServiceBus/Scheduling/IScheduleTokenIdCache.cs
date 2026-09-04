using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Defines the contract for schedule token id cache.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IScheduleTokenIdCache<in T>
    where T : class
{
    /// <summary>
    /// Try to get the tokenId for the scheduler from the message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="tokenId"></param>
    /// <returns></returns>
    bool TryGetTokenId(T message, out Guid tokenId);
}
