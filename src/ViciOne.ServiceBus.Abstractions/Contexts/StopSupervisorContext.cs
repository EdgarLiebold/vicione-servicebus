namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for stop supervisor operations.</summary>
public interface StopSupervisorContext :
    StopContext
{
    /// <summary>The agents available when the Stop was initiated.</summary>
    IAgent[] Agents { get; }
}
