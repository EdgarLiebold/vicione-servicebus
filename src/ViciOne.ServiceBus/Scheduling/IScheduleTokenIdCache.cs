using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Provides cached access to schedule token id data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IScheduleTokenIdCache<in T>
    where T : class
{
    /// <summary>Try to get the tokenId for the scheduler from the message.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="tokenId">Receives the token id produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetTokenId(T message, out Guid tokenId);
}
