using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides a message correlation implementation.
/// </summary>
public static class MessageCorrelation
{
    /// <summary>
    /// Configures correlation id for the current pipeline.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="getCorrelationId">The get correlation id value.</param>
    public static void UseCorrelationId<T>(Func<T, Guid> getCorrelationId)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getCorrelationId);
        GlobalTopology.UseCorrelationId(getCorrelationId);
    }

    /// <summary>
    /// Configures correlation id for the current pipeline.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="getCorrelationId">The get correlation id value.</param>
    public static void UseCorrelationId<T>(Func<T, Guid?> getCorrelationId)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getCorrelationId);
        GlobalTopology.UseCorrelationId(getCorrelationId);
    }
}
