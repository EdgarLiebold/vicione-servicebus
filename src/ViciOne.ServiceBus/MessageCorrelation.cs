using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines correlation for message.</summary>
public static class MessageCorrelation
{
    /// <summary>Configures correlation id for the current pipeline.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="getCorrelationId">The get correlation id.</param>
    public static void UseCorrelationId<T>(Func<T, Guid> getCorrelationId)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getCorrelationId);
        GlobalTopology.UseCorrelationId(getCorrelationId);
    }

    /// <summary>Configures correlation id for the current pipeline.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="getCorrelationId">The get correlation id.</param>
    public static void UseCorrelationId<T>(Func<T, Guid?> getCorrelationId)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getCorrelationId);
        GlobalTopology.UseCorrelationId(getCorrelationId);
    }
}
