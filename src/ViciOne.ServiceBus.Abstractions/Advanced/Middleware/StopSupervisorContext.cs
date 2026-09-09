namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Describes a supervisor stop attempt and the child agents captured by it.</summary>
public interface StopSupervisorContext :
    StopContext
{
    /// <summary>Gets the child agents that were active when the stop attempt began.</summary>
    IAgent[] Agents { get; }
}
