namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a schedule token id implementation.
/// </summary>
public static class ScheduleTokenId
{
    /// <summary>
    /// Configures token id for the current pipeline.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="tokenIdSelector">The token id selector value.</param>
    public static void UseTokenId<T>(ScheduleTokenIdCache<T>.TokenIdSelector tokenIdSelector)
        where T : class
    {
        ScheduleTokenIdCache<T>.UseTokenId(tokenIdSelector);
    }
}
