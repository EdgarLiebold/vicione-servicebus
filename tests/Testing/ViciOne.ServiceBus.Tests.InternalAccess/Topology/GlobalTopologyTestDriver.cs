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
}
