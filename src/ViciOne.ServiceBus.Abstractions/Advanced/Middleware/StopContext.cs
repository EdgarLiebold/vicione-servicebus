namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Describes why an agent is stopping and carries the shutdown cancellation budget.</summary>
public interface StopContext :
    PipeContext
{
    /// <summary>Gets the reason for the stop request.</summary>
    string Reason { get; }
}
