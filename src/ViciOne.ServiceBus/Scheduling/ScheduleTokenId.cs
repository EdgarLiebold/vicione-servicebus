namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Represents the identifier for schedule token.</summary>
public static class ScheduleTokenId
{
    /// <summary>Configures token id for the current pipeline.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="tokenIdSelector">The token id selector.</param>
    public static void UseTokenId<T>(ScheduleTokenIdCache<T>.TokenIdSelector tokenIdSelector)
        where T : class
    {
        ScheduleTokenIdCache<T>.UseTokenId(tokenIdSelector);
    }
}
