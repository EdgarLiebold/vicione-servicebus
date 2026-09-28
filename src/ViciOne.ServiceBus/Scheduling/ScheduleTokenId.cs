using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Configures how scheduling tokens are obtained from message contracts.</summary>
public static class ScheduleTokenId
{
    /// <summary>Registers the process-wide token selector for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="tokenIdSelector">Returns a non-empty existing scheduling token, or <see langword="null" /> to generate one.</param>
    public static void UseTokenId<T>(Func<T, Guid?> tokenIdSelector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(tokenIdSelector);
        ScheduleTokenIdCache<T>.UseTokenId(tokenIdSelector);
    }
}
