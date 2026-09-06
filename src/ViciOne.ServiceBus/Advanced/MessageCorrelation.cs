using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Configures global correlation identifiers for message contracts.</summary>
public static class MessageCorrelation
{
    /// <summary>Configures a required correlation identifier for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="getCorrelationId">Selects the correlation identifier from a message.</param>
    public static void UseCorrelationId<T>(Func<T, Guid> getCorrelationId)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getCorrelationId);
        GlobalTopology.UseCorrelationId(getCorrelationId);
    }

    /// <summary>Configures an optional correlation identifier for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="getCorrelationId">Selects the correlation identifier from a message.</param>
    public static void UseCorrelationId<T>(Func<T, Guid?> getCorrelationId)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getCorrelationId);
        GlobalTopology.UseCorrelationId(getCorrelationId);
    }
}
