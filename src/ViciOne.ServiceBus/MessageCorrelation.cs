using System;

namespace ViciOne.ServiceBus;

public static class MessageCorrelation
{
    public static void UseCorrelationId<T>(Func<T, Guid> getCorrelationId)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getCorrelationId);
        GlobalTopology.UseCorrelationId(getCorrelationId);
    }

    public static void UseCorrelationId<T>(Func<T, Guid?> getCorrelationId)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getCorrelationId);
        GlobalTopology.UseCorrelationId(getCorrelationId);
    }
}
