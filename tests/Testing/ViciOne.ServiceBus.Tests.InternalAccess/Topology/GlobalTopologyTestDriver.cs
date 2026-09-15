namespace ViciOne.ServiceBus.Tests.InternalAccess.Topology;

/// <summary>Exercises the internal capability-convention boundary without exposing it as product API.</summary>
public static class GlobalTopologyTestDriver
{
    public static bool RegisterCapabilityCorrelationId<T>(Func<T, Guid> selector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(selector);
        GlobalTopology.UseCapabilityCorrelationId(selector);
        return GlobalTopology.Send
            .GetMessageTopology<T>()
            .TryGetConvention(out ICorrelationIdMessageSendTopologyConvention<T>? convention)
            && convention is not null;
    }

    public static bool TryResolveCapabilityNullableCorrelationId<T>(
        Func<T, Guid?> selector,
        T message,
        out Guid correlationId)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(message);

        GlobalTopology.UseCapabilityCorrelationId(selector);

        if (GlobalTopology.Send.GetMessageTopology<T>()
                .TryGetConvention(out ICorrelationIdMessageSendTopologyConvention<T>? convention)
            && convention is not null
            && convention.TryGetCorrelationIdResolver(out IMessageCorrelationId<T>? resolver))
            return resolver.TryGetCorrelationId(message, out correlationId);

        correlationId = Guid.Empty;
        return false;
    }
}
