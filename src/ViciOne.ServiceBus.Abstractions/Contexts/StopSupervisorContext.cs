namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for stop supervisor context.
/// </summary>
public interface StopSupervisorContext :
    StopContext
{
    /// <summary>
    /// The agents available when the Stop was initiated
    /// </summary>
    IAgent[] Agents { get; }
}
