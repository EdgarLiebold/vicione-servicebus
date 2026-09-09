using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Describes a supervisor stop attempt and the child agents captured by it.</summary>
public interface StopSupervisorContext :
    StopContext
{
    /// <summary>Gets a read-only snapshot of the child agents active when the stop attempt began.</summary>
    IReadOnlyList<IAgent> Agents { get; }
}
